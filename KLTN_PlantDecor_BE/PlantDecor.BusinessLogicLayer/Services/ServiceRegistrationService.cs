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

            var nurseryCareService = await _unitOfWork.NurseryCareServiceRepository.GetByIdWithDetailsAsync(request.NurseryCareServiceId);
            if (nurseryCareService == null)
                throw new NotFoundException($"NurseryCareService {request.NurseryCareServiceId} not found");

            if (!nurseryCareService.IsActive)
                throw new BadRequestException("This care service is not currently active");

            if (request.ServiceDate < DateOnly.FromDateTime(DateTime.Today))
                throw new BadRequestException("ServiceDate cannot be in the past");

            var preferredShift = await _unitOfWork.ShiftRepository.GetByIdAsync(request.PreferredShiftId);
            if (preferredShift == null)
                throw new NotFoundException($"Shift {request.PreferredShiftId} not found");

            var pkg = nurseryCareService.CareServicePackage;
            if (pkg == null)
                throw new BadRequestException("Care service package not found for this nursery care service");

            if (!pkg.DurationDays.HasValue || !pkg.ServiceType.HasValue)
                throw new BadRequestException("Care service package is missing configuration");

            bool isOneTime = pkg.ServiceType.Value == (int)CareServiceTypeEnum.OneTime;
            var minimumLeadHours = isOneTime ? 24 : 48;
            var firstSessionStartAt = request.ServiceDate.ToDateTime(preferredShift.StartTime);

            if (firstSessionStartAt < DateTime.Now.AddHours(minimumLeadHours))
                throw new BadRequestException($"{(isOneTime ? "One-time" : "Periodic")} service must be booked at least {minimumLeadHours} hours in advance");

            int totalSessions;
            string scheduleDaysJson;
            var scheduleDaysOfWeek = request.ScheduleDaysOfWeek ?? new List<int>();

            if (isOneTime)
            {
                // Dịch vụ one-time: 1 session duy nhất vào ngày ServiceDate, không cần ScheduleDaysOfWeek
                totalSessions = 1;
                scheduleDaysJson = JsonSerializer.Serialize(new List<int>());
            }
            else
            {
                if (!pkg.VisitPerWeek.HasValue)
                    throw new BadRequestException("Periodic care service package is missing VisitPerWeek configuration");

                if (!scheduleDaysOfWeek.Any())
                    throw new BadRequestException("ScheduleDaysOfWeek is required for periodic service package");

                if (scheduleDaysOfWeek.Any(d => d < 1 || d > 6))
                    throw new BadRequestException("ScheduleDaysOfWeek only accepts values 1-6 (Monday to Saturday), Sunday is not supported");

                if (scheduleDaysOfWeek.Count != pkg.VisitPerWeek!.Value)
                    throw new BadRequestException($"You must select exactly {pkg.VisitPerWeek.Value} day(s) per week as per this package");

                totalSessions = (int)Math.Ceiling(pkg.DurationDays.Value / 7.0) * pkg.VisitPerWeek.Value;
                scheduleDaysJson = JsonSerializer.Serialize(scheduleDaysOfWeek);
            }

            var registration = new ServiceRegistration
            {
                UserId = userId,
                NurseryCareServiceId = request.NurseryCareServiceId,
                PreferredShiftId = request.PreferredShiftId,
                ServiceDate = request.ServiceDate,
                ScheduleDaysOfWeek = scheduleDaysJson,
                TotalSessions = totalSessions,
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

            // Get all caretakers in the nursery
            var allStaff = await _unitOfWork.UserRepository.GetCaretakersByNurseryIdAsync(nursery.Id);

            // Load package with required specializations explicitly to avoid missing include chains.
            var packageId = registration.NurseryCareService?.CareServicePackageId
                ?? throw new BadRequestException("Registration is missing care service package");

            var pkg = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(packageId);
            if (pkg == null)
                throw new NotFoundException($"CareServicePackage {packageId} not found");

            // Filter by specializations required by the package
            IEnumerable<User> eligibleStaff;
            if (pkg?.CareServiceSpecializations != null && pkg.CareServiceSpecializations.Count > 0)
            {
                var requiredSpecIds = pkg.CareServiceSpecializations.Select(cs => cs.SpecializationId).ToHashSet();
                eligibleStaff = allStaff
                    .Where(u => requiredSpecIds.All(reqId => u.StaffSpecializations.Any(ss => ss.SpecializationId == reqId)));
            }
            else
            {
                eligibleStaff = allStaff;
            }

            // Filter out caretakers with schedule conflicts
            if (registration.PreferredShiftId.HasValue)
            {
                var sessionDates = ComputeSessionDates(registration);
                if (sessionDates.Count > 0)
                {
                    var conflictingIds = await _unitOfWork.ServiceProgressRepository
                        .GetConflictingCaretakerIdsAsync(registration.PreferredShiftId.Value, sessionDates);
                    eligibleStaff = eligibleStaff.Where(u => !conflictingIds.Contains(u.Id));
                }
            }

            return eligibleStaff.Select(NurseryService.MapToStaffDtoPublic).ToList();
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
