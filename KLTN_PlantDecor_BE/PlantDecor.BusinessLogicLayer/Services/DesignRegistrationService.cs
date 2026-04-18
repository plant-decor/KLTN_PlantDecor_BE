using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Http;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class DesignRegistrationService : IDesignRegistrationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public DesignRegistrationService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<DesignRegistrationResponseDto> CreateAsync(int userId, CreateDesignRegistrationRequestDto request)
        {
            if (request == null)
                throw new BadRequestException("Request body is required");

            if (string.IsNullOrWhiteSpace(request.Address))
                throw new BadRequestException("Address is required");

            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new BadRequestException("Phone is required");

            var nursery = await _unitOfWork.NurseryRepository.GetByIdAsync(request.NurseryId)
                ?? throw new NotFoundException($"Nursery {request.NurseryId} not found");

            if (nursery.IsActive != true)
                throw new BadRequestException("Selected nursery is inactive");

            var tier = await _unitOfWork.DesignTemplateTierRepository.GetByIdAsync(request.DesignTemplateTierId)
                ?? throw new NotFoundException($"DesignTemplateTier {request.DesignTemplateTierId} not found");

            if (!tier.IsActive)
                throw new BadRequestException("Selected design template tier is inactive");

            var nurseryDesignTemplates = await _unitOfWork.NurseryDesignTemplateRepository.GetAllAsync();
            var isTemplateSupported = nurseryDesignTemplates.Any(x =>
                x.NurseryId == request.NurseryId
                && x.DesignTemplateId == tier.DesignTemplateId
                && x.IsActive);

            if (!isTemplateSupported)
                throw new BadRequestException("Selected nursery does not offer this design template");

            var totalPrice = tier.PackagePrice;
            var depositAmount = Math.Round(totalPrice * 0.3m, 2, MidpointRounding.AwayFromZero);

            var registration = new DesignRegistration
            {
                UserId = userId,
                NurseryId = request.NurseryId,
                DesignTemplateTierId = request.DesignTemplateTierId,
                TotalPrice = totalPrice,
                DepositAmount = depositAmount,
                Latitude = null,
                Longitude = null,
                Width = null,
                Length = null,
                CurrentStateImageUrl = null,
                Address = request.Address.Trim(),
                Phone = request.Phone.Trim(),
                CustomerNote = string.IsNullOrWhiteSpace(request.CustomerNote) ? null : request.CustomerNote.Trim(),
                Status = (int)DesignRegistrationStatus.PendingApproval,
                CreatedAt = DateTime.Now
            };

            _unitOfWork.DesignRegistrationRepository.PrepareCreate(registration);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(registration.Id)
                ?? throw new NotFoundException($"DesignRegistration {registration.Id} not found after create");

            return MapToDto(created);
        }

        public async Task<PaginatedResult<DesignRegistrationResponseDto>> GetMyRegistrationsAsync(int userId, Pagination pagination, int? status = null)
        {
            var result = await _unitOfWork.DesignRegistrationRepository.GetByUserIdAsync(userId, pagination, status);
            return new PaginatedResult<DesignRegistrationResponseDto>(
                result.Items.Select(MapToDto).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<DesignRegistrationResponseDto> GetByIdAsync(int id, int requesterId)
        {
            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (registration.UserId != requesterId && registration.AssignedCaretakerId != requesterId)
                throw new ForbiddenException("You don't have access to this registration");

            return MapToDto(registration);
        }

        public async Task<PaginatedResult<DesignRegistrationResponseDto>> GetAllForNurseryAsync(int managerId, Pagination pagination, int? status = null)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);
            var result = await _unitOfWork.DesignRegistrationRepository.GetByNurseryIdAsync(nursery.Id, pagination, status);

            return new PaginatedResult<DesignRegistrationResponseDto>(
                result.Items.Select(MapToDto).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<DesignRegistrationResponseDto> GetByIdAsOperatorAsync(int managerId, int id)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (registration.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            return MapToDto(registration);
        }

        public async Task<DesignRegistrationResponseDto> ApproveAsync(int managerId, int id)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registrationDetail = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (registrationDetail.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registrationDetail.Status != (int)DesignRegistrationStatus.PendingApproval)
                throw new BadRequestException("Only pending registrations can be approved");

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Create Order
                var order = new Order
                {
                    UserId = registration.UserId,
                    OrderType = (int)OrderTypeEnum.Design,
                    Status = (int)OrderStatusEnum.Pending,
                    PaymentStrategy = (int)PaymentStrategiesEnum.Deposit,
                    TotalAmount = registration.TotalPrice,
                    DepositAmount = registration.DepositAmount,
                    RemainingAmount = registration.TotalPrice - registration.DepositAmount,
                    Address = registration.Address,
                    Phone = registration.Phone,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _unitOfWork.OrderRepository.PrepareCreate(order);
                await _unitOfWork.SaveAsync(); // order.Id is now populated

                // 2. Create Deposit Invoice
                var tierName = registrationDetail.DesignTemplateTier?.TierName ?? "Design Service";
                var invoice = new Invoice
                {
                    OrderId = order.Id,
                    Type = (int)InvoiceTypeEnum.Deposit,
                    TotalAmount = registration.DepositAmount,
                    Status = (int)InvoiceStatusEnum.Pending,
                    IssuedDate = DateTime.Now,
                    InvoiceDetails = new List<InvoiceDetail>
                    {
                        new InvoiceDetail
                        {
                            ItemName = $"Design service deposit - {tierName}",
                            UnitPrice = registration.DepositAmount,
                            Quantity = 1,
                            Amount = registration.DepositAmount
                        }
                    }
                };
                _unitOfWork.InvoiceRepository.PrepareCreate(invoice);

                // 3. Update Registration
                registration.OrderId = order.Id;
                registration.Status = (int)DesignRegistrationStatus.AwaitDeposit;
                registration.ApprovedAt = DateTime.Now;
                _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var updated = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found after approve");

            return MapToDto(updated);
        }

        public async Task<DesignRegistrationResponseDto> RejectAsync(int managerId, int id, string? rejectReason)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registrationDetail = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (registrationDetail.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registrationDetail.Status != (int)DesignRegistrationStatus.PendingApproval)
                throw new BadRequestException("Only pending registrations can be rejected");

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            registration.Status = (int)DesignRegistrationStatus.Rejected;
            registration.CancelReason = string.IsNullOrWhiteSpace(rejectReason) ? "Rejected by nursery" : rejectReason.Trim();

            _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found after reject");

            return MapToDto(updated);
        }

        public async Task<DesignRegistrationResponseDto> AssignCaretakerAsync(int managerId, int id, int caretakerId)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registrationDetail = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (registrationDetail.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registrationDetail.Status != (int)DesignRegistrationStatus.AwaitDeposit
                && registrationDetail.Status != (int)DesignRegistrationStatus.Active)
                throw new BadRequestException("Can only assign caretaker when registration is AwaitDeposit or Active");

            var caretaker = await _unitOfWork.UserRepository.GetByIdAsync(caretakerId)
                ?? throw new NotFoundException($"User {caretakerId} not found");

            if (caretaker.RoleId != (int)RoleEnum.Caretaker)
                throw new BadRequestException("Selected user is not a caretaker");

            if (caretaker.Status != (int)UserStatusEnum.Active || !caretaker.IsVerified)
                throw new BadRequestException("Caretaker account is not active or verified");

            if (!caretaker.NurseryId.HasValue || caretaker.NurseryId.Value != nursery.Id)
                throw new ForbiddenException("Caretaker is not assigned to your nursery");

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            registration.AssignedCaretakerId = caretakerId;
            _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);

            var tasks = await _unitOfWork.DesignTaskRepository.GetByRegistrationIdAsync(id);
            foreach (var task in tasks)
            {
                if (task.Status == (int)DesignTaskStatusEnum.Completed || task.Status == (int)DesignTaskStatusEnum.Cancelled)
                    continue;

                var trackedTask = await _unitOfWork.DesignTaskRepository.GetByIdAsync(task.Id);
                if (trackedTask == null)
                    continue;

                trackedTask.AssignedStaffId = caretakerId;

                if (trackedTask.Status == (int)DesignTaskStatusEnum.Pending)
                {
                    trackedTask.Status = (int)DesignTaskStatusEnum.Assigned;
                }

                _unitOfWork.DesignTaskRepository.PrepareUpdate(trackedTask);
            }

            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found after assign caretaker");

            return MapToDto(updated);
        }

        public async Task<DesignRegistrationResponseDto> UpdateSurveyInfoAsync(int caretakerId, int id, UpdateDesignRegistrationSurveyInfoRequestDto request, IFormFile? currentStateImage = null)
        {
            if (request == null)
                throw new BadRequestException("Request body is required");

            if (!request.Latitude.HasValue && !request.Longitude.HasValue
                && !request.Width.HasValue && !request.Length.HasValue
                && currentStateImage == null)
                throw new BadRequestException("At least one survey field or current-state image must be provided");

            var registrationDetail = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (registrationDetail.AssignedCaretakerId != caretakerId)
                throw new ForbiddenException("Only assigned caretaker can update survey info");

            if (registrationDetail.Status != (int)DesignRegistrationStatus.Active)
                throw new BadRequestException("Survey info can only be updated when registration is Active");

            if (request.Latitude.HasValue != request.Longitude.HasValue)
                throw new BadRequestException("Latitude and Longitude must be provided together");

            if (request.Width.HasValue != request.Length.HasValue)
                throw new BadRequestException("Width and Length must be provided together");

            if (request.Latitude.HasValue && (request.Latitude.Value < -90 || request.Latitude.Value > 90))
                throw new BadRequestException("Latitude must be between -90 and 90");

            if (request.Longitude.HasValue && (request.Longitude.Value < -180 || request.Longitude.Value > 180))
                throw new BadRequestException("Longitude must be between -180 and 180");

            if (request.Width.HasValue && request.Width.Value <= 0)
                throw new BadRequestException("Width must be greater than 0");

            if (request.Length.HasValue && request.Length.Value <= 0)
                throw new BadRequestException("Length must be greater than 0");

            if (request.Width.HasValue && request.Length.HasValue)
            {
                var area = request.Width.Value * request.Length.Value;
                var tier = registrationDetail.DesignTemplateTier
                    ?? throw new BadRequestException("DesignTemplateTier is missing on registration");

                if (area < tier.MinArea || area > tier.MaxArea)
                    throw new BadRequestException($"Area must be between {tier.MinArea} and {tier.MaxArea} for this tier");
            }

            if (currentStateImage != null)
            {
                var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(currentStateImage);
                if (!isValid)
                    throw new BadRequestException(errorMessage);
            }

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (request.Latitude.HasValue)
                registration.Latitude = request.Latitude.Value;

            if (request.Longitude.HasValue)
                registration.Longitude = request.Longitude.Value;

            if (request.Width.HasValue)
                registration.Width = request.Width.Value;

            if (request.Length.HasValue)
                registration.Length = request.Length.Value;

            if (currentStateImage != null)
            {
                var uploadResult = await _cloudinaryService.UploadFileAsync(currentStateImage, "DesignRegistrationCurrentState");
                registration.CurrentStateImageUrl = uploadResult.SecureUrl;
            }

            _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found after survey update");

            return MapToDto(updated);
        }

        public async Task<DesignRegistrationResponseDto> CancelAsync(int userId, int id, string? cancelReason)
        {
            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (registration.UserId != userId)
                throw new ForbiddenException("You don't have access to this registration");

            if (registration.Status != (int)DesignRegistrationStatus.PendingApproval
                && registration.Status != (int)DesignRegistrationStatus.AwaitDeposit)
                throw new BadRequestException("You can only cancel registrations in PendingApproval or AwaitDeposit status");

            var tracked = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            tracked.Status = (int)DesignRegistrationStatus.Cancelled;
            tracked.CancelReason = string.IsNullOrWhiteSpace(cancelReason) ? "Cancelled by customer" : cancelReason.Trim();

            _unitOfWork.DesignRegistrationRepository.PrepareUpdate(tracked);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found after cancel");

            return MapToDto(updated);
        }

        public async Task<DesignRegistrationResponseDto> ManagerCancelAsync(int managerId, int id, string? cancelReason)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registrationDetail = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (registrationDetail.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registrationDetail.Status == (int)DesignRegistrationStatus.Cancelled
                || registrationDetail.Status == (int)DesignRegistrationStatus.Completed
                || registrationDetail.Status == (int)DesignRegistrationStatus.Rejected)
                throw new BadRequestException("Registration is already finalized and cannot be cancelled");

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            registration.Status = (int)DesignRegistrationStatus.Cancelled;
            registration.CancelReason = string.IsNullOrWhiteSpace(cancelReason) ? "Cancelled by nursery" : cancelReason.Trim();
            _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);

            var tasks = await _unitOfWork.DesignTaskRepository.GetByRegistrationIdAsync(id);
            foreach (var task in tasks)
            {
                if (task.Status == (int)DesignTaskStatusEnum.Completed || task.Status == (int)DesignTaskStatusEnum.Cancelled)
                    continue;

                var trackedTask = await _unitOfWork.DesignTaskRepository.GetByIdAsync(task.Id);
                if (trackedTask == null)
                    continue;

                trackedTask.Status = (int)DesignTaskStatusEnum.Cancelled;
                _unitOfWork.DesignTaskRepository.PrepareUpdate(trackedTask);
            }

            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found after manager cancel");

            return MapToDto(updated);
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

        public static DesignRegistrationResponseDto MapToDto(DesignRegistration registration)
        {
            return new DesignRegistrationResponseDto
            {
                Id = registration.Id,
                UserId = registration.UserId,
                OrderId = registration.OrderId,
                NurseryId = registration.NurseryId,
                DesignTemplateTierId = registration.DesignTemplateTierId,
                AssignedCaretakerId = registration.AssignedCaretakerId,
                TotalPrice = registration.TotalPrice,
                DepositAmount = registration.DepositAmount,
                Latitude = registration.Latitude,
                Longitude = registration.Longitude,
                Width = registration.Width,
                Length = registration.Length,
                CurrentStateImageUrl = registration.CurrentStateImageUrl,
                Address = registration.Address,
                Phone = registration.Phone,
                CustomerNote = registration.CustomerNote,
                CancelReason = registration.CancelReason,
                Status = registration.Status,
                StatusName = Enum.IsDefined(typeof(DesignRegistrationStatus), registration.Status)
                    ? ((DesignRegistrationStatus)registration.Status).ToString()
                    : $"Unknown({registration.Status})",
                CreatedAt = registration.CreatedAt,
                ApprovedAt = registration.ApprovedAt,
                Customer = registration.User == null ? null : ServiceRegistrationService.MapUserSummary(registration.User),
                AssignedCaretaker = registration.AssignedCaretaker == null ? null : ServiceRegistrationService.MapUserSummary(registration.AssignedCaretaker),
                Nursery = registration.Nursery == null ? null : new DesignNurserySummaryDto
                {
                    Id = registration.Nursery.Id,
                    Name = registration.Nursery.Name
                },
                DesignTemplateTier = registration.DesignTemplateTier == null ? null : new DesignTemplateTierSummaryDto
                {
                    Id = registration.DesignTemplateTier.Id,
                    TierName = registration.DesignTemplateTier.TierName,
                    MinArea = registration.DesignTemplateTier.MinArea,
                    MaxArea = registration.DesignTemplateTier.MaxArea,
                    PackagePrice = registration.DesignTemplateTier.PackagePrice,
                    EstimatedDays = registration.DesignTemplateTier.EstimatedDays,
                    ScopedOfWork = registration.DesignTemplateTier.ScopedOfWork,
                    DesignTemplate = registration.DesignTemplateTier.DesignTemplate == null ? null : new DesignTemplateSummaryDto
                    {
                        Id = registration.DesignTemplateTier.DesignTemplate.Id,
                        Name = registration.DesignTemplateTier.DesignTemplate.Name,
                        Description = registration.DesignTemplateTier.DesignTemplate.Description,
                        ImageUrl = registration.DesignTemplateTier.DesignTemplate.ImageUrl,
                        Style = registration.DesignTemplateTier.DesignTemplate.Style,
                        RoomTypes = registration.DesignTemplateTier.DesignTemplate.RoomTypes
                    }
                },
                DesignTasks = registration.DesignTasks
                    .OrderBy(x => x.ScheduledDate)
                    .ThenBy(x => x.Id)
                    .Select(DesignTaskService.MapToDto)
                    .ToList()
            };
        }
    }
}
