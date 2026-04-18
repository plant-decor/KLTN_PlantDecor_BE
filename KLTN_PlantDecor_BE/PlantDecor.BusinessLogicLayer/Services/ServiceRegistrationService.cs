using System.Text.Json;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ServiceRegistrationService : IServiceRegistrationService
    {
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
            User? selectedCaretaker = null;

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

            foreach (var candidateService in candidateServices)
            {
                var eligibleCaretakers = await GetEligibleCaretakersForNurseryAndPackageAsync(
                    candidateService.NurseryId,
                    selectedPackage,
                    request.PreferredShiftId,
                    sessionDates);

                if (!eligibleCaretakers.Any())
                    continue;

                selectedService = candidateService;
                selectedCaretaker = eligibleCaretakers.First();
                break;
            }

            if (selectedService == null || selectedCaretaker == null)
                throw new BadRequestException("No suitable NurseryCareService with available caretakers was found for your requested schedule");

            var registration = new ServiceRegistration
            {
                UserId = userId,
                NurseryCareServiceId = selectedService.Id,
                MainCaretakerId = selectedCaretaker.Id,
                CurrentCaretakerId = selectedCaretaker.Id,
                PreferredShiftId = request.PreferredShiftId,
                ServiceDate = request.ServiceDate,
                ScheduleDaysOfWeek = selectedScheduleDaysJson,
                TotalSessions = selectedTotalSessions,
                Address = request.Address,
                Phone = request.Phone,
                Note = request.Note,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Status = (int)ServiceRegistrationStatusEnum.PendingApproval,
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
            return MapToDto(created!);
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
            var minimumLeadHours = isOneTime ? 24 : 48;
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
                result.Items.Select(MapToDto).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<PaginatedResult<ServiceRegistrationResponseDto>> GetPendingForNurseryAsync(int managerId, Pagination pagination)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var result = await _unitOfWork.ServiceRegistrationRepository.GetPendingByNurseryIdAsync(nursery.Id, pagination);
            return new PaginatedResult<ServiceRegistrationResponseDto>(
                result.Items.Select(MapToDto).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<PaginatedResult<ServiceRegistrationResponseDto>> GetAllForNurseryAsync(int managerId, Pagination pagination, int? status = null)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var result = await _unitOfWork.ServiceRegistrationRepository.GetAllByNurseryIdAsync(nursery.Id, pagination, status);
            return new PaginatedResult<ServiceRegistrationResponseDto>(
                result.Items.Select(MapToDto).ToList(),
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

            return MapToDto(registration);
        }

        public async Task<ServiceRegistrationResponseDto> GetByIdAsManagerAsync(int managerId, int id)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            return MapToDto(registration);
        }

        public async Task<ServiceRegistrationResponseDto> ApproveAsync(int managerId, int id)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registration.Status != (int)ServiceRegistrationStatusEnum.PendingApproval)
                throw new BadRequestException($"Registration is not in PendingApproval status");

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
            return MapToDto(updated!);
        }

        public async Task<ServiceRegistrationResponseDto> RejectAsync(int managerId, int id, string? rejectReason)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registration.Status != (int)ServiceRegistrationStatusEnum.PendingApproval)
                throw new BadRequestException("Only registrations in PendingApproval status can be rejected");

            registration.Status = (int)ServiceRegistrationStatusEnum.Rejected;
            registration.CancelReason = rejectReason;
            _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            return MapToDto(updated!);
        }

        public async Task<ServiceRegistrationResponseDto> AssignCaretakerAsync(int managerId, int id, int caretakerId)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registration.Status != (int)ServiceRegistrationStatusEnum.AwaitPayment &&
                registration.Status != (int)ServiceRegistrationStatusEnum.Active)
                throw new BadRequestException("Can only assign caretaker when registration is AwaitPayment or Active");

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
            return MapToDto(updated!);
        }

        public async Task<ServiceRegistrationResponseDto> CancelAsync(int userId, int id, string? cancelReason)
        {
            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.UserId != userId)
                throw new ForbiddenException("You don't have access to this registration");

            if (registration.Status != (int)ServiceRegistrationStatusEnum.PendingApproval &&
                registration.Status != (int)ServiceRegistrationStatusEnum.AwaitPayment)
                throw new BadRequestException("Only registrations in PendingApproval or AwaitPayment status can be cancelled");

            registration.Status = (int)ServiceRegistrationStatusEnum.Cancelled;
            registration.CancelReason = cancelReason;
            _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            return MapToDto(updated!);
        }

        public async Task<PaginatedResult<ServiceRegistrationResponseDto>> GetMyTasksAsync(int caretakerId, Pagination pagination, int? status)
        {
            var result = await _unitOfWork.ServiceRegistrationRepository.GetByCaretakerIdAsync(caretakerId, pagination, status);
            return new PaginatedResult<ServiceRegistrationResponseDto>(
                result.Items.Select(MapToDto).ToList(),
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
                throw new BadRequestException("Manager can only cancel registrations that are Active. Use reject for PendingApproval.");

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
            return MapToDto(updated!);
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
            List<DateOnly> sessionDates)
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

                eligible = eligible.Where(u => !conflictingIds.Contains(u.Id));
            }

            return eligible.OrderBy(u => u.Username).ToList();
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


        #region Mapping

        public static ServiceRegistrationResponseDto MapToDto(ServiceRegistration r)
        {
            return new ServiceRegistrationResponseDto
            {
                Id = r.Id,
                Status = r.Status,
                StatusName = r.Status.HasValue ? ((ServiceRegistrationStatusEnum)r.Status.Value).ToString() : null,
                ServiceDate = r.ServiceDate,
                TotalSessions = r.TotalSessions,
                Address = r.Address,
                Phone = r.Phone,
                Note = r.Note,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                ScheduleDaysOfWeek = r.ScheduleDaysOfWeek,
                CancelReason = r.CancelReason,
                CreatedAt = r.CreatedAt,
                ApprovedAt = r.ApprovedAt,
                OrderId = r.OrderId,
                NurseryCareService = r.NurseryCareService == null ? null : new NurseryCareServiceSummaryDto
                {
                    Id = r.NurseryCareService.Id,
                    NurseryId = r.NurseryCareService.NurseryId,
                    NurseryName = r.NurseryCareService.Nursery?.Name,
                    CareServicePackage = r.NurseryCareService.CareServicePackage == null ? null : new CareServicePackageSummaryDto
                    {
                        Id = r.NurseryCareService.CareServicePackage.Id,
                        Name = r.NurseryCareService.CareServicePackage.Name,
                        Description = r.NurseryCareService.CareServicePackage.Description,
                        VisitPerWeek = r.NurseryCareService.CareServicePackage.VisitPerWeek,
                        DurationDays = r.NurseryCareService.CareServicePackage.DurationDays,
                        ServiceType = r.NurseryCareService.CareServicePackage.ServiceType,
                        UnitPrice = r.NurseryCareService.CareServicePackage.UnitPrice,
                    }
                },
                PrefferedShift = r.PrefferedShift == null ? null : new ShiftSummaryDto
                {
                    Id = r.PrefferedShift.Id,
                    ShiftName = r.PrefferedShift.ShiftName,
                    StartTime = r.PrefferedShift.StartTime,
                    EndTime = r.PrefferedShift.EndTime
                },
                Customer = r.User == null ? null : MapUserSummary(r.User),
                MainCaretaker = r.MainCaretaker == null ? null : MapUserSummary(r.MainCaretaker),
                CurrentCaretaker = r.CurrentCaretaker == null ? null : MapUserSummary(r.CurrentCaretaker),
                Progresses = r.ServiceProgresses
                    .OrderBy(sp => sp.TaskDate)
                    .Select(ServiceProgressService.MapToDto)
                    .ToList(),
                Rating = r.ServiceRating == null ? null : ServiceRatingService.MapToDto(r.ServiceRating)
            };
        }

        public static UserSummaryDto MapUserSummary(User user) => new UserSummaryDto
        {
            Id = user.Id,
            FullName = user.Username,
            Email = user.Email,
            Phone = user.PhoneNumber,
            Avatar = user.AvatarUrl
        };

        #endregion
    }
}
