using System.Text.Json;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ServiceRegistrationService : IServiceRegistrationService
    {
        private const string RejectRouteMetaPrefix = "__route_meta__:";

        private readonly IUnitOfWork _unitOfWork;

        public ServiceRegistrationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceRegistrationResponseDto> CreateAsync(int userId, CreateServiceRegistrationRequestDto request)
        {
            if (request == null)
                throw new BadRequestException("Request body is required");

            if (request.ServiceDate < DateOnly.FromDateTime(DateTime.Today))
                throw new BadRequestException("ServiceDate cannot be in the past");

            var preferredShift = await _unitOfWork.ShiftRepository.GetByIdAsync(request.PreferredShiftId);
            if (preferredShift == null)
                throw new NotFoundException($"Shift {request.PreferredShiftId} not found");

            var selectedPackage = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(request.CareServicePackageId)
                ?? throw new NotFoundException($"CareServicePackage {request.CareServicePackageId} not found");

            if (selectedPackage.IsActive != true)
                throw new BadRequestException("This care service package is not currently active");

            var sessionDates = BuildScheduleFromPackageOrThrow(
                selectedPackage,
                request,
                preferredShift,
                out var selectedTotalSessions,
                out var selectedScheduleDaysJson);

            NurseryCareService? selectedService = null;

            var candidateServices = new List<NurseryCareService>();

            if (request.PreferredNurseryId.HasValue)
            {
                var preferredNursery = await _unitOfWork.NurseryRepository.GetByIdAsync(request.PreferredNurseryId.Value);
                if (preferredNursery == null || preferredNursery.IsActive != true)
                    throw new NotFoundException($"Preferred nursery {request.PreferredNurseryId.Value} not found or inactive");

                var servicesInNursery = await _unitOfWork.NurseryCareServiceRepository.GetByNurseryIdAsync(preferredNursery.Id);
                var preferredService = servicesInNursery.FirstOrDefault(s => s.CareServicePackageId == request.CareServicePackageId && s.IsActive);
                if (preferredService == null)
                    throw new BadRequestException("Preferred nursery does not offer the selected service package");

                candidateServices.Add(preferredService);
            }
            else
            {
                // Auto mode: hệ thống tự chọn NurseryCareService phù hợp theo vị trí + nhân sự rảnh
                if (!request.Latitude.HasValue || !request.Longitude.HasValue)
                    throw new BadRequestException("Latitude and Longitude are required for automatic nursery assignment");

                var nearbyNurseries = await _unitOfWork.NurseryRepository.GetNearbyWithPackageAsync(
                    request.Latitude.Value,
                    request.Longitude.Value,
                    30000,
                    request.CareServicePackageId);

                foreach (var nursery in nearbyNurseries)
                {
                    var service = nursery.NurseryCareServices
                        .FirstOrDefault(s => s.IsActive && s.CareServicePackageId == request.CareServicePackageId);
                    if (service != null)
                        candidateServices.Add(service);
                }
            }

            if (!candidateServices.Any())
                throw new BadRequestException("No nursery is available for the selected service package at the moment");

            selectedService = await SelectBestNurseryServiceAsync(
                selectedPackage,
                request.PreferredShiftId,
                sessionDates,
                candidateServices);

            var initialStatus = ServiceRegistrationStatusEnum.PendingApproval;
            if (selectedService == null)
            {
                var fallbackCandidateServices = await FilterServicesByQualifiedCaretakersAsync(
                    candidateServices,
                    selectedPackage);

                selectedService = await SelectFallbackNurseryServiceAsync(
                    fallbackCandidateServices,
                    request.Latitude,
                    request.Longitude);

                if (selectedService == null)
                    throw new BadRequestException("No nursery has qualified caretakers for the selected service package at the moment");

                initialStatus = ServiceRegistrationStatusEnum.WaitingForNursery;
            }

            var registration = new ServiceRegistration
            {
                UserId = userId,
                NurseryCareServiceId = selectedService.Id,
                MainCaretakerId = null,
                CurrentCaretakerId = null,
                PreferredShiftId = request.PreferredShiftId,
                ServiceDate = request.ServiceDate,
                ScheduleDaysOfWeek = selectedScheduleDaysJson,
                TotalSessions = selectedTotalSessions,
                Address = request.Address,
                Phone = request.Phone,
                Note = request.Note,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                CancelReason = BuildCancelReasonWithRouteMeta(
                    new HashSet<int>(),
                    request.PreferredNurseryId.HasValue,
                    null),
                Status = (int)initialStatus,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.ServiceRegistrationRepository.PrepareCreate(registration);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var created = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(registration.Id);
            return created!.ToResponse();
        }

        private static List<DateOnly> BuildScheduleFromPackageOrThrow(
            CareServicePackage pkg,
            CreateServiceRegistrationRequestDto request,
            Shift preferredShift,
            out int totalSessions,
            out string scheduleDaysJson)
        {
            if (!TryBuildScheduleFromPackage(
                    pkg,
                    request,
                    preferredShift,
                    out totalSessions,
                    out scheduleDaysJson,
                    out var sessionDates,
                    strictValidation: true))
            {
                throw new BadRequestException("Unable to build schedule from selected package");
            }

            return sessionDates;
        }

        private static bool TryBuildScheduleFromPackage(
            CareServicePackage pkg,
            CreateServiceRegistrationRequestDto request,
            Shift preferredShift,
            out int totalSessions,
            out string scheduleDaysJson,
            out List<DateOnly> sessionDates,
            bool strictValidation = false)
        {
            totalSessions = 0;
            scheduleDaysJson = JsonSerializer.Serialize(new List<int>());
            sessionDates = new List<DateOnly>();

            if (!pkg.DurationDays.HasValue || !pkg.ServiceType.HasValue)
            {
                if (strictValidation)
                    throw new BadRequestException("Care service package is missing configuration");
                return false;
            }

            bool isOneTime = pkg.ServiceType.Value == (int)CareServiceTypeEnum.OneTime;
            var minimumLeadHours = isOneTime ? 6 : 24;
            var firstSessionStartAt = request.ServiceDate.ToDateTime(preferredShift.StartTime);

            if (firstSessionStartAt < DateTime.Now.AddHours(minimumLeadHours))
            {
                if (strictValidation)
                    throw new BadRequestException($"{(isOneTime ? "One-time" : "Periodic")} service must be booked at least {minimumLeadHours} hours in advance");
                return false;
            }

            var scheduleDaysOfWeek = (request.ScheduleDaysOfWeek ?? new List<int>()).Distinct().ToList();

            if (!strictValidation)
            {
                if (scheduleDaysOfWeek.Any() && isOneTime)
                    return false;
                if (!scheduleDaysOfWeek.Any() && !isOneTime)
                    return false;
            }

            if (isOneTime)
            {
                totalSessions = 1;
                sessionDates = new List<DateOnly> { request.ServiceDate };
                scheduleDaysJson = JsonSerializer.Serialize(new List<int>());
                return true;
            }

            if (!pkg.VisitPerWeek.HasValue)
            {
                if (strictValidation)
                    throw new BadRequestException("Periodic care service package is missing VisitPerWeek configuration");
                return false;
            }

            if (!scheduleDaysOfWeek.Any())
            {
                if (strictValidation)
                    throw new BadRequestException("ScheduleDaysOfWeek is required for periodic service package");
                return false;
            }

            if (scheduleDaysOfWeek.Any(d => d < 1 || d > 6))
            {
                if (strictValidation)
                    throw new BadRequestException("ScheduleDaysOfWeek only accepts values 1-6 (Monday to Saturday), Sunday is not supported");
                return false;
            }

            if (scheduleDaysOfWeek.Count != pkg.VisitPerWeek.Value)
            {
                if (strictValidation)
                    throw new BadRequestException($"You must select exactly {pkg.VisitPerWeek.Value} day(s) per week as per this package");
                return false;
            }

            totalSessions = (int)Math.Ceiling(pkg.DurationDays.Value / 7.0) * pkg.VisitPerWeek.Value;
            scheduleDaysJson = JsonSerializer.Serialize(scheduleDaysOfWeek);
            sessionDates = ComputeSessionDates(request.ServiceDate, totalSessions, false, scheduleDaysOfWeek);
            return true;
        }

        public async Task<PaginatedResult<ServiceRegistrationResponseDto>> GetMyRegistrationsAsync(int userId, Pagination pagination, int? status)
        {
            var result = await _unitOfWork.ServiceRegistrationRepository.GetByUserIdAsync(userId, pagination, status);
            return new PaginatedResult<ServiceRegistrationResponseDto>(
                result.Items.Select(x => x.ToResponse()).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<PaginatedResult<ServiceRegistrationResponseDto>> GetPendingForNurseryAsync(int managerId, Pagination pagination)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var result = await _unitOfWork.ServiceRegistrationRepository.GetPendingByNurseryIdAsync(nursery.Id, pagination);
            return new PaginatedResult<ServiceRegistrationResponseDto>(
                result.Items.Select(x => x.ToResponse()).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<PaginatedResult<ServiceRegistrationResponseDto>> GetAllForNurseryAsync(int managerId, Pagination pagination, int? status = null)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var result = await _unitOfWork.ServiceRegistrationRepository.GetAllByNurseryIdAsync(nursery.Id, pagination, status);
            return new PaginatedResult<ServiceRegistrationResponseDto>(
                result.Items.Select(x => x.ToResponse()).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<ServiceRegistrationResponseDto> GetByIdAsync(int id, int userId)
        {
            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            // Customer can view their own; caretaker can view their assigned tasks; admins/managers handled at controller level
            if (registration.UserId != userId &&
                registration.MainCaretakerId != userId &&
                registration.CurrentCaretakerId != userId)
                throw new ForbiddenException("You don't have access to this registration");

            return registration.ToResponse();
        }

        public async Task<ServiceRegistrationResponseDto> GetByIdAsManagerAsync(int managerId, int id)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            return registration.ToResponse();
        }

        public async Task<ServiceRegistrationResponseDto> ApproveAsync(int managerId, int id)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registration.Status != (int)ServiceRegistrationStatusEnum.PendingApproval
                && registration.Status != (int)ServiceRegistrationStatusEnum.WaitingForNursery)
                throw new BadRequestException("Only registrations in WaitingForNursery or PendingApproval status can be approved");

            var pkg = registration.NurseryCareService!.CareServicePackage;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Create Order
                var order = new Order
                {
                    UserId = registration.UserId!.Value,
                    OrderType = (int)OrderTypeEnum.Service,
                    Status = (int)OrderStatusEnum.Pending,
                    PaymentStrategy = (int)PaymentStrategiesEnum.FullPayment,
                    TotalAmount = pkg.UnitPrice ?? 0,
                    Address = registration.Address,
                    Phone = registration.Phone,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _unitOfWork.OrderRepository.PrepareCreate(order);
                await _unitOfWork.SaveAsync(); // order.Id is now populated

                // 2. Create Invoice (OrderId is now available)
                var invoice = new Invoice
                {
                    OrderId = order.Id,
                    Type = (int)InvoiceTypeEnum.FullPayment,
                    TotalAmount = order.TotalAmount,
                    Status = (int)InvoiceStatusEnum.Pending,
                    IssuedDate = DateTime.Now,
                    InvoiceDetails = new List<InvoiceDetail>
                    {
                        new InvoiceDetail
                        {
                            ItemName = pkg.Name ?? "Care Service",
                            UnitPrice = pkg.UnitPrice ?? 0,
                            Quantity = 1,
                            Amount = pkg.UnitPrice ?? 0
                        }
                    }
                };
                _unitOfWork.InvoiceRepository.PrepareCreate(invoice);

                // 3. Update Registration
                registration.OrderId = order.Id;
                registration.Status = (int)ServiceRegistrationStatusEnum.AwaitPayment;
                registration.ApprovedAt = DateTime.Now;
                _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var updated = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            return updated!.ToResponse();
        }

        public async Task<ServiceRegistrationResponseDto> RejectAsync(int managerId, int id, string? rejectReason)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registration.Status != (int)ServiceRegistrationStatusEnum.PendingApproval
                && registration.Status != (int)ServiceRegistrationStatusEnum.WaitingForNursery)
                throw new BadRequestException("Only registrations in WaitingForNursery or PendingApproval status can be rejected");

            var (routeMeta, _) = ParseRejectRouteMeta(registration.CancelReason);
            var packageId = registration.NurseryCareService?.CareServicePackageId;
            var currentNurseryId = registration.NurseryCareService?.NurseryId;
            var rejectedNurseryHistory = routeMeta.RejectedNurseryIds;
            var isPreferredNurseryRequested = routeMeta.IsPreferredNurseryRequested;

            if (currentNurseryId.HasValue)
            {
                rejectedNurseryHistory.Add(currentNurseryId.Value);
            }

            var shouldTryRematch = !isPreferredNurseryRequested
                                   && registration.Status == (int)ServiceRegistrationStatusEnum.PendingApproval;

            if (shouldTryRematch && packageId.HasValue && currentNurseryId.HasValue)
            {
                var package = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(packageId.Value);
                if (package != null)
                {
                    var sessionDates = ComputeSessionDates(registration);
                    var candidateServices = await BuildCandidateServicesForRerouteAsync(registration, packageId.Value);

                    var nextService = await SelectBestNurseryServiceAsync(
                        package,
                        registration.PreferredShiftId,
                        sessionDates,
                        candidateServices,
                        excludeNurseryIds: rejectedNurseryHistory);

                    if (nextService != null)
                    {
                        registration.NurseryCareServiceId = nextService.Id;
                        registration.MainCaretakerId = null;
                        registration.CurrentCaretakerId = null;
                        registration.CancelReason = BuildCancelReasonWithRouteMeta(
                            rejectedNurseryHistory,
                            isPreferredNurseryRequested,
                            null);
                        registration.Status = (int)ServiceRegistrationStatusEnum.PendingApproval;

                        _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);
                        await _unitOfWork.SaveAsync();

                        var rerouted = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
                        return rerouted!.ToResponse();
                    }
                }
            }

            var normalizedRejectReason = string.IsNullOrWhiteSpace(rejectReason)
                ? null
                : rejectReason.Trim();

            if (normalizedRejectReason == null)
            {
                if (isPreferredNurseryRequested)
                {
                    normalizedRejectReason = "Preferred nursery rejected this registration due to caretaker overload";
                }
                else if (registration.Status == (int)ServiceRegistrationStatusEnum.WaitingForNursery)
                {
                    normalizedRejectReason = "All nurseries are currently overloaded and cannot accept this registration";
                }
                else
                {
                    normalizedRejectReason = "Registration rejected because no nursery currently has enough caretaker capacity";
                }
            }

            registration.Status = (int)ServiceRegistrationStatusEnum.Rejected;
            registration.CancelReason = BuildCancelReasonWithRouteMeta(
                rejectedNurseryHistory,
                isPreferredNurseryRequested,
                normalizedRejectReason);
            _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            return updated!.ToResponse();
        }

        public async Task<ServiceRegistrationResponseDto> AssignCaretakerAsync(int managerId, int id, int caretakerId)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registration.Status != (int)ServiceRegistrationStatusEnum.Active)
                throw new BadRequestException("Can only assign caretaker when registration is Active");

            var caretaker = await _unitOfWork.UserRepository.GetByIdAsync(caretakerId);
            if (caretaker == null)
                throw new NotFoundException($"User {caretakerId} not found");

            if (caretaker.RoleId != (int)RoleEnum.Caretaker)
                throw new BadRequestException("Selected user is not a caretaker");

            if (caretaker.Status != (int)UserStatusEnum.Active || !caretaker.IsVerified)
                throw new BadRequestException("Caretaker account is not active or verified");

            if (!caretaker.NurseryId.HasValue || caretaker.NurseryId.Value != nursery.Id)
                throw new ForbiddenException("Caretaker is not assigned to your nursery");

            // Check schedule conflict: 1 ca chỉ làm 1 đơn
            if (registration.PreferredShiftId.HasValue)
            {
                var sessionDates = ComputeSessionDates(registration);
                if (sessionDates.Count > 0)
                {
                    var conflictingIds = await _unitOfWork.ServiceProgressRepository
                        .GetConflictingCaretakerIdsAsync(registration.PreferredShiftId.Value, sessionDates);
                    if (conflictingIds.Contains(caretakerId))
                        throw new BadRequestException("Caretaker has schedule conflicts on one or more sessions of this registration");
                }
            }

            registration.MainCaretakerId = caretakerId;
            registration.CurrentCaretakerId = caretakerId;
            _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);

            foreach (var progress in registration.ServiceProgresses
                         .Where(sp => sp.Status != (int)ServiceProgressStatusEnum.Completed &&
                                      sp.Status != (int)ServiceProgressStatusEnum.Cancelled))
            {
                progress.CaretakerId = caretakerId;
                if (progress.Status == (int)ServiceProgressStatusEnum.Pending ||
                    progress.Status == (int)ServiceProgressStatusEnum.Assigned)
                    progress.Status = (int)ServiceProgressStatusEnum.Assigned;

                _unitOfWork.ServiceProgressRepository.PrepareUpdate(progress);
            }

            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            return updated!.ToResponse();
        }

        public async Task<ServiceRegistrationResponseDto> RescheduleAsync(int managerId, int id, UpdateServiceRegistrationScheduleRequestDto request)
        {
            if (request == null)
                throw new BadRequestException("Request body is required");

            if (!request.ServiceDate.HasValue && !request.PreferredShiftId.HasValue)
                throw new BadRequestException("At least ServiceDate or PreferredShiftId must be provided");

            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registrationDetail = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registrationDetail.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registrationDetail.Status != (int)ServiceRegistrationStatusEnum.WaitingForNursery
                && registrationDetail.Status != (int)ServiceRegistrationStatusEnum.PendingApproval
                && registrationDetail.Status != (int)ServiceRegistrationStatusEnum.AwaitPayment)
            {
                throw new BadRequestException("Only registrations in WaitingForNursery, PendingApproval or AwaitPayment can be rescheduled");
            }

            var targetServiceDate = request.ServiceDate ?? registrationDetail.ServiceDate;
            var targetShiftId = request.PreferredShiftId ?? registrationDetail.PreferredShiftId;

            if (!targetServiceDate.HasValue)
                throw new BadRequestException("ServiceDate is missing on registration and must be provided");

            if (!targetShiftId.HasValue)
                throw new BadRequestException("PreferredShiftId is missing on registration and must be provided");

            var targetShift = await _unitOfWork.ShiftRepository.GetByIdAsync(targetShiftId.Value)
                ?? throw new NotFoundException($"Shift {targetShiftId.Value} not found");

            var packageId = registrationDetail.NurseryCareService?.CareServicePackageId
                ?? throw new BadRequestException("Registration is missing care service package");

            var pkg = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(packageId)
                ?? throw new NotFoundException($"CareServicePackage {packageId} not found");

            var scheduleDaysOfWeek = string.IsNullOrWhiteSpace(registrationDetail.ScheduleDaysOfWeek)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(registrationDetail.ScheduleDaysOfWeek) ?? new List<int>();

            var validationRequest = new CreateServiceRegistrationRequestDto
            {
                CareServicePackageId = packageId,
                PreferredShiftId = targetShiftId.Value,
                ServiceDate = targetServiceDate.Value,
                ScheduleDaysOfWeek = scheduleDaysOfWeek,
                Address = registrationDetail.Address ?? string.Empty,
                Phone = registrationDetail.Phone ?? "0900000000",
                Note = registrationDetail.Note,
                Latitude = registrationDetail.Latitude,
                Longitude = registrationDetail.Longitude
            };

            _ = BuildScheduleFromPackageOrThrow(
                pkg,
                validationRequest,
                targetShift,
                out var totalSessions,
                out var scheduleDaysJson);

            var trackedRegistration = await _unitOfWork.ServiceRegistrationRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"ServiceRegistration {id} not found");

            trackedRegistration.ServiceDate = targetServiceDate.Value;
            trackedRegistration.PreferredShiftId = targetShiftId.Value;
            trackedRegistration.TotalSessions = totalSessions;
            trackedRegistration.ScheduleDaysOfWeek = scheduleDaysJson;

            _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(trackedRegistration);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"ServiceRegistration {id} not found after reschedule");

            return updated.ToResponse();
        }

        public async Task<ServiceRegistrationResponseDto> CancelAsync(int userId, int id, string? cancelReason)
        {
            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.UserId != userId)
                throw new ForbiddenException("You don't have access to this registration");

            if (registration.Status != (int)ServiceRegistrationStatusEnum.WaitingForNursery &&
                registration.Status != (int)ServiceRegistrationStatusEnum.PendingApproval &&
                registration.Status != (int)ServiceRegistrationStatusEnum.AwaitPayment)
                throw new BadRequestException("Only registrations in WaitingForNursery, PendingApproval or AwaitPayment status can be cancelled");

            registration.Status = (int)ServiceRegistrationStatusEnum.Cancelled;
            registration.CancelReason = cancelReason;
            _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            return updated!.ToResponse();
        }

        public async Task<PaginatedResult<ServiceRegistrationResponseDto>> GetMyTasksAsync(int caretakerId, Pagination pagination, int? status)
        {
            var result = await _unitOfWork.ServiceRegistrationRepository.GetByCaretakerIdAsync(caretakerId, pagination, status);
            return new PaginatedResult<ServiceRegistrationResponseDto>(
                result.Items.Select(x => x.ToResponse()).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<ServiceRegistrationResponseDto> ManagerCancelAsync(int managerId, int id, string? cancelReason)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registration.Status != (int)ServiceRegistrationStatusEnum.Active)
                throw new BadRequestException("Manager can only cancel registrations that are Active. Use reject for WaitingForNursery/PendingApproval.");

            // Cancel all pending/assigned service progress sessions
            foreach (var progress in registration.ServiceProgresses
                .Where(sp => sp.Status != (int)ServiceProgressStatusEnum.Completed &&
                             sp.Status != (int)ServiceProgressStatusEnum.Cancelled))
            {
                progress.Status = (int)ServiceProgressStatusEnum.Cancelled;
                _unitOfWork.ServiceProgressRepository.PrepareUpdate(progress);
            }

            registration.Status = (int)ServiceRegistrationStatusEnum.Cancelled;
            registration.CancelReason = cancelReason;
            _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);

            // Update Order to Cancelled
            if (registration.OrderId.HasValue)
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(registration.OrderId.Value);
                if (order != null)
                {
                    order.Status = (int)OrderStatusEnum.Cancelled;
                    order.UpdatedAt = DateTime.Now;
                    _unitOfWork.OrderRepository.PrepareUpdate(order);
                }
            }

            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            return updated!.ToResponse();
        }

        public async Task<List<StaffWithSpecializationsResponseDto>> GetEligibleCaretakersForRegistrationAsync(int managerId, int registrationId)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(registrationId);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {registrationId} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            // Load package with required specializations explicitly to avoid missing include chains.
            var packageId = registration.NurseryCareService?.CareServicePackageId
                ?? throw new BadRequestException("Registration is missing care service package");

            var pkg = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(packageId);
            if (pkg == null)
                throw new NotFoundException($"CareServicePackage {packageId} not found");

            var sessionDates = ComputeSessionDates(registration);
            var eligibleStaff = await GetEligibleCaretakersForNurseryAndPackageAsync(
                nursery.Id,
                pkg,
                registration.PreferredShiftId,
                sessionDates);

            return eligibleStaff.Select(NurseryService.MapToStaffDtoPublic).ToList();
        }

        private async Task<List<User>> GetEligibleCaretakersForNurseryAndPackageAsync(
            int nurseryId,
            CareServicePackage pkg,
            int? preferredShiftId,
            List<DateOnly> sessionDates,
            bool sortByWorkload = true)
        {
            var allCaretakers = await _unitOfWork.UserRepository.GetCaretakersByNurseryIdAsync(nurseryId);

            IEnumerable<User> eligible = allCaretakers
                .Where(u => u.Status == (int)UserStatusEnum.Active && u.IsVerified);

            if (pkg.CareServiceSpecializations != null && pkg.CareServiceSpecializations.Count > 0)
            {
                var requiredSpecIds = pkg.CareServiceSpecializations
                    .Select(cs => cs.SpecializationId)
                    .ToHashSet();

                eligible = eligible.Where(u => requiredSpecIds.All(reqId =>
                    u.StaffSpecializations.Any(ss => ss.SpecializationId == reqId)));
            }

            if (preferredShiftId.HasValue && sessionDates.Count > 0)
            {
                var conflictingIds = await _unitOfWork.ServiceProgressRepository
                    .GetConflictingCaretakerIdsAsync(preferredShiftId.Value, sessionDates);

                eligible = eligible.Where(u => !conflictingIds.Contains(u.Id)).ToList();
            }

            if (!eligible.Any())
                return new List<User>();

            var eligibleList = eligible
                .OrderBy(u => u.Username ?? string.Empty)
                .ThenBy(u => u.Id)
                .ToList();

            if (!sortByWorkload)
                return eligibleList;

            var workloads = await _unitOfWork.ServiceRegistrationRepository
                .CountOpenAssignmentsByCaretakerIdsAsync(
                    eligibleList.Select(u => u.Id).ToList(),
                    new List<int> { nurseryId });

            return eligibleList
                .OrderBy(u => workloads.TryGetValue(u.Id, out var count) ? count : 0)
                .ThenBy(u => u.Username)
                .ThenBy(u => u.Id)
                .ToList();
        }

        private async Task<List<NurseryCareService>> BuildCandidateServicesForRerouteAsync(ServiceRegistration registration, int packageId)
        {
            var candidates = new List<NurseryCareService>();

            if (registration.Latitude.HasValue && registration.Longitude.HasValue)
            {
                var nearbyNurseries = await _unitOfWork.NurseryRepository.GetNearbyWithPackageAsync(
                    registration.Latitude.Value,
                    registration.Longitude.Value,
                    30000,
                    packageId);

                foreach (var nursery in nearbyNurseries)
                {
                    var service = nursery.NurseryCareServices
                        .FirstOrDefault(s => s.IsActive && s.CareServicePackageId == packageId);
                    if (service != null)
                    {
                        candidates.Add(service);
                    }
                }

                if (candidates.Any())
                {
                    return candidates;
                }
            }

            return await _unitOfWork.NurseryCareServiceRepository.GetActiveByPackageIdAsync(packageId);
        }

        private async Task<NurseryCareService?> SelectBestNurseryServiceAsync(
            CareServicePackage package,
            int? preferredShiftId,
            List<DateOnly> sessionDates,
            List<NurseryCareService> candidateServices,
            ISet<int>? excludeNurseryIds = null)
        {
            var scoredCandidates = new List<(NurseryCareService Service, int EligibleCount, int MinWorkload, int TotalWorkload)>();
            var eligibleByServiceId = new Dictionary<int, List<User>>();
            var allCaretakerIds = new HashSet<int>();
            var allNurseryIds = new HashSet<int>();

            foreach (var candidateService in candidateServices)
            {
                if (excludeNurseryIds != null && excludeNurseryIds.Contains(candidateService.NurseryId))
                {
                    continue;
                }

                var eligibleCaretakers = await GetEligibleCaretakersForNurseryAndPackageAsync(
                    candidateService.NurseryId,
                    package,
                    preferredShiftId,
                    sessionDates,
                    sortByWorkload: false);

                if (!eligibleCaretakers.Any())
                {
                    continue;
                }

                eligibleByServiceId[candidateService.Id] = eligibleCaretakers;
                allNurseryIds.Add(candidateService.NurseryId);
                foreach (var caretaker in eligibleCaretakers)
                {
                    allCaretakerIds.Add(caretaker.Id);
                }
            }

            if (!eligibleByServiceId.Any())
            {
                return null;
            }

            var workloads = await _unitOfWork.ServiceRegistrationRepository
                .CountOpenAssignmentsByCaretakerIdsAsync(
                    allCaretakerIds.ToList(),
                    allNurseryIds.ToList());

            foreach (var candidateService in candidateServices)
            {
                if (!eligibleByServiceId.TryGetValue(candidateService.Id, out var eligibleCaretakers))
                {
                    continue;
                }

                var caretakerIds = eligibleCaretakers.Select(c => c.Id).ToList();
                var loads = caretakerIds
                    .Select(id => workloads.TryGetValue(id, out var count) ? count : 0)
                    .ToList();

                scoredCandidates.Add((
                    candidateService,
                    EligibleCount: caretakerIds.Count,
                    MinWorkload: loads.Min(),
                    TotalWorkload: loads.Sum()));
            }

            return scoredCandidates
                .OrderByDescending(x => x.EligibleCount)
                .ThenBy(x => x.MinWorkload)
                .ThenBy(x => x.TotalWorkload)
                .ThenBy(x => x.Service.NurseryId)
                .Select(x => x.Service)
                .FirstOrDefault();
        }

        private Task<NurseryCareService?> SelectFallbackNurseryServiceAsync(
            List<NurseryCareService> candidateServices,
            decimal? latitude,
            decimal? longitude)
        {
            if (candidateServices == null || candidateServices.Count == 0)
            {
                return Task.FromResult<NurseryCareService?>(null);
            }

            var useDistance = latitude.HasValue && longitude.HasValue;

            var selected = candidateServices
                .OrderBy(s => useDistance
                    ? HaversineKm(latitude!.Value, longitude!.Value, s.Nursery?.Latitude, s.Nursery?.Longitude)
                    : double.MaxValue)
                .ThenBy(s => s.NurseryId)
                .ThenBy(s => s.Id)
                .FirstOrDefault();

            return Task.FromResult(selected);
        }

        private async Task<List<NurseryCareService>> FilterServicesByQualifiedCaretakersAsync(
            List<NurseryCareService> candidateServices,
            CareServicePackage package)
        {
            if (candidateServices == null || candidateServices.Count == 0)
            {
                return new List<NurseryCareService>();
            }

            var filtered = new List<NurseryCareService>();

            foreach (var candidateService in candidateServices)
            {
                var caretakerPool = await _unitOfWork.UserRepository.GetCaretakersByNurseryIdAsync(candidateService.NurseryId);

                IEnumerable<User> qualified = caretakerPool
                    .Where(u => u.Status == (int)UserStatusEnum.Active && u.IsVerified);

                if (package.CareServiceSpecializations != null && package.CareServiceSpecializations.Count > 0)
                {
                    var requiredSpecIds = package.CareServiceSpecializations
                        .Select(cs => cs.SpecializationId)
                        .ToHashSet();

                    qualified = qualified.Where(u => requiredSpecIds.All(reqId =>
                        u.StaffSpecializations.Any(ss => ss.SpecializationId == reqId)));
                }

                if (qualified.Any())
                {
                    filtered.Add(candidateService);
                }
            }

            return filtered;
        }

        private static double HaversineKm(decimal lat1, decimal lon1, decimal? lat2, decimal? lon2)
        {
            if (!lat2.HasValue || !lon2.HasValue)
            {
                return double.MaxValue;
            }

            const double earthRadiusKm = 6371.0;

            var dLat = (double)(lat2.Value - lat1) * Math.PI / 180.0;
            var dLon = (double)(lon2.Value - lon1) * Math.PI / 180.0;
            var originLat = (double)lat1 * Math.PI / 180.0;
            var targetLat = (double)lat2.Value * Math.PI / 180.0;

            var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0)
                    + Math.Cos(originLat) * Math.Cos(targetLat)
                    * Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

            return earthRadiusKm * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        }

        private async Task<Nursery> ResolveOperatorNurseryAsync(int operatorId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(operatorId);
            if (nursery != null)
                return nursery;

            var user = await _unitOfWork.UserRepository.GetByIdAsync(operatorId);
            if (user?.RoleId == (int)RoleEnum.Staff && user.NurseryId.HasValue)
            {
                var staffNursery = await _unitOfWork.NurseryRepository.GetByIdAsync(user.NurseryId.Value);
                if (staffNursery != null)
                    return staffNursery;
            }

            throw new ForbiddenException("You are not a manager/staff of any nursery");
        }

        private sealed class RejectRouteMeta
        {
            public HashSet<int> RejectedNurseryIds { get; init; } = new();
            public bool IsPreferredNurseryRequested { get; init; }
        }

        private sealed class RejectRouteMetaPayload
        {
            public List<int>? RejectedNurseryIds { get; set; }
            public bool? IsPreferredNurseryRequested { get; set; }
        }

        private static (RejectRouteMeta Meta, string? UserReason) ParseRejectRouteMeta(string? cancelReason)
        {
            if (string.IsNullOrWhiteSpace(cancelReason)
                || !cancelReason.StartsWith(RejectRouteMetaPrefix, StringComparison.Ordinal))
            {
                return (new RejectRouteMeta(), cancelReason);
            }

            var payload = cancelReason.Substring(RejectRouteMetaPrefix.Length);
            var separatorIndex = payload.IndexOf('|');
            var jsonPart = separatorIndex >= 0 ? payload.Substring(0, separatorIndex) : payload;
            var userReason = separatorIndex >= 0 && separatorIndex < payload.Length - 1
                ? payload.Substring(separatorIndex + 1)
                : null;

            try
            {
                var parsedPayload = JsonSerializer.Deserialize<RejectRouteMetaPayload>(jsonPart);
                if (parsedPayload != null &&
                    (parsedPayload.RejectedNurseryIds != null || parsedPayload.IsPreferredNurseryRequested.HasValue))
                {
                    return (new RejectRouteMeta
                    {
                        RejectedNurseryIds = (parsedPayload.RejectedNurseryIds ?? new List<int>())
                            .Where(id => id > 0)
                            .ToHashSet(),
                        IsPreferredNurseryRequested = parsedPayload.IsPreferredNurseryRequested == true
                    }, userReason);
                }
            }
            catch
            {
                // Fallback to legacy format below.
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<List<int>>(jsonPart) ?? new List<int>();
                return (new RejectRouteMeta
                {
                    RejectedNurseryIds = parsed.Where(id => id > 0).ToHashSet(),
                    IsPreferredNurseryRequested = false
                }, userReason);
            }
            catch
            {
                return (new RejectRouteMeta(), userReason);
            }
        }

        private static string? BuildCancelReasonWithRouteMeta(
            HashSet<int> rejectedNurseryHistory,
            bool isPreferredNurseryRequested,
            string? userReason)
        {
            var normalizedReason = string.IsNullOrWhiteSpace(userReason) ? null : userReason.Trim();

            if (rejectedNurseryHistory.Count == 0 && !isPreferredNurseryRequested)
            {
                return normalizedReason;
            }

            var payload = new RejectRouteMetaPayload
            {
                RejectedNurseryIds = rejectedNurseryHistory.OrderBy(id => id).ToList(),
                IsPreferredNurseryRequested = isPreferredNurseryRequested ? true : null
            };
            var historyJson = JsonSerializer.Serialize(payload);
            return normalizedReason == null
                ? $"{RejectRouteMetaPrefix}{historyJson}"
                : $"{RejectRouteMetaPrefix}{historyJson}|{normalizedReason}";
        }

        private static string? ExtractUserReasonFromStoredCancelReason(string? storedCancelReason)
        {
            if (string.IsNullOrWhiteSpace(storedCancelReason))
            {
                return null;
            }

            if (!storedCancelReason.StartsWith(RejectRouteMetaPrefix, StringComparison.Ordinal))
            {
                return storedCancelReason;
            }

            var payload = storedCancelReason.Substring(RejectRouteMetaPrefix.Length);
            var separatorIndex = payload.IndexOf('|');

            if (separatorIndex < 0 || separatorIndex >= payload.Length - 1)
            {
                return null;
            }

            return payload.Substring(separatorIndex + 1);
        }

        private static string? ResolveDisplayCancelReason(int? status, string? storedCancelReason)
        {
            if (status != (int)ServiceRegistrationStatusEnum.Rejected &&
                status != (int)ServiceRegistrationStatusEnum.Cancelled)
            {
                return null;
            }

            return ExtractUserReasonFromStoredCancelReason(storedCancelReason);
        }

        private static List<DateOnly> ComputeSessionDates(ServiceRegistration r)
        {
            var dates = new List<DateOnly>();
            if (r.ServiceDate == null || r.TotalSessions == null) return dates;

            bool isOneTime = r.NurseryCareService?.CareServicePackage?.ServiceType == (int)CareServiceTypeEnum.OneTime;
            if (isOneTime)
            {
                dates.Add(r.ServiceDate.Value);
                return dates;
            }

            var scheduleDays = string.IsNullOrEmpty(r.ScheduleDaysOfWeek)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(r.ScheduleDaysOfWeek) ?? new List<int>();

            var current = r.ServiceDate.Value;
            var generated = 0;
            while (generated < r.TotalSessions.Value)
            {
                // DayOfWeek: Sunday=0, Monday=1,...Saturday=6  — matches our enum (1=Mon..6=Sat)
                int dayCode = (int)current.DayOfWeek;
                if (scheduleDays.Contains(dayCode))
                {
                    dates.Add(current);
                    generated++;
                }
                current = current.AddDays(1);
            }
            return dates;
        }

        private static List<DateOnly> ComputeSessionDates(DateOnly startDate, int totalSessions, bool isOneTime, List<int> scheduleDays)
        {
            if (isOneTime)
                return new List<DateOnly> { startDate };

            var dates = new List<DateOnly>();
            var current = startDate;

            while (dates.Count < totalSessions)
            {
                int dayCode = (int)current.DayOfWeek;
                if (scheduleDays.Contains(dayCode))
                    dates.Add(current);

                current = current.AddDays(1);
            }

            return dates;
        }


    }
}
