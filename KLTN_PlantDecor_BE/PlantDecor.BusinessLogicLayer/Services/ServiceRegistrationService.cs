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
            var nurseryCareService = await _unitOfWork.NurseryCareServiceRepository.GetByIdWithDetailsAsync(request.NurseryCareServiceId);
            if (nurseryCareService == null)
                throw new NotFoundException($"NurseryCareService {request.NurseryCareServiceId} not found");

            if (!nurseryCareService.IsActive)
                throw new BadRequestException("This care service is not currently active");

            if (request.ServiceDate < DateOnly.FromDateTime(DateTime.Today))
                throw new BadRequestException("ServiceDate cannot be in the past");

            var pkg = nurseryCareService.CareServicePackage;

            if (!pkg.DurationDays.HasValue || !pkg.ServiceType.HasValue)
                throw new BadRequestException("Care service package is missing configuration");

            bool isOneTime = pkg.ServiceType.Value == (int)CareServiceTypeEnum.OneTime;

            int totalSessions;
            string scheduleDaysJson;

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

                if (request.ScheduleDaysOfWeek.Any(d => d < 1 || d > 6))
                    throw new BadRequestException("ScheduleDaysOfWeek only accepts values 1-6 (Monday to Saturday), Sunday is not supported");

                if (request.ScheduleDaysOfWeek.Count != pkg.VisitPerWeek!.Value)
                    throw new BadRequestException($"You must select exactly {pkg.VisitPerWeek.Value} day(s) per week as per this package");

                totalSessions = (int)Math.Ceiling(pkg.DurationDays.Value / 7.0) * pkg.VisitPerWeek.Value;
                scheduleDaysJson = JsonSerializer.Serialize(request.ScheduleDaysOfWeek);
            }

            var registration = new ServiceRegistration
            {
                UserId = userId,
                NurseryCareServiceId = request.NurseryCareServiceId,
                PrefferedShiftId = request.PrefferedShiftId,
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

            _unitOfWork.ServiceRegistrationRepository.PrepareCreate(registration);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(registration.Id);
            return MapToDto(created!);
        }

        public async Task<PaginatedResult<ServiceRegistrationResponseDto>> GetMyRegistrationsAsync(int userId, Pagination pagination)
        {
            var result = await _unitOfWork.ServiceRegistrationRepository.GetByUserIdAsync(userId, pagination);
            return new PaginatedResult<ServiceRegistrationResponseDto>(
                result.Items.Select(MapToDto).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<PaginatedResult<ServiceRegistrationResponseDto>> GetPendingForNurseryAsync(int managerId, Pagination pagination)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var result = await _unitOfWork.ServiceRegistrationRepository.GetPendingByNurseryIdAsync(nursery.Id, pagination);
            return new PaginatedResult<ServiceRegistrationResponseDto>(
                result.Items.Select(MapToDto).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<PaginatedResult<ServiceRegistrationResponseDto>> GetAllForNurseryAsync(int managerId, Pagination pagination, int? status = null)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

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
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            return MapToDto(registration);
        }

        public async Task<ServiceRegistrationResponseDto> ApproveAsync(int managerId, int id)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            if (registration == null)
                throw new NotFoundException($"ServiceRegistration {id} not found");

            if (registration.NurseryCareService?.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registration.Status != (int)ServiceRegistrationStatusEnum.PendingApproval)
                throw new BadRequestException($"Registration is not in PendingApproval status");

            var pkg = registration.NurseryCareService!.CareServicePackage;

            // Create Order
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
            await _unitOfWork.SaveAsync();

            // Create Invoice
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

            // Update registration
            registration.OrderId = order.Id;
            registration.Status = (int)ServiceRegistrationStatusEnum.AwaitPayment;
            registration.ApprovedAt = DateTime.Now;
            _unitOfWork.ServiceRegistrationRepository.PrepareUpdate(registration);

            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(id);
            return MapToDto(updated!);
        }

        public async Task<ServiceRegistrationResponseDto> RejectAsync(int managerId, int id, string? rejectReason)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

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
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

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
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

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
