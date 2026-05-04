using System.Text.Json;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using Microsoft.AspNetCore.Http;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class DesignRegistrationService : IDesignRegistrationService
    {
        private const string RejectRouteMetaPrefix = "__route_meta__:";
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

            var tier = await _unitOfWork.DesignTemplateTierRepository.GetByIdAsync(request.DesignTemplateTierId)
                ?? throw new NotFoundException($"DesignTemplateTier {request.DesignTemplateTierId} not found");

            if (!tier.IsActive)
                throw new BadRequestException("Selected design template tier is inactive");

            var nurseryTemplateMappings = await _unitOfWork.NurseryDesignTemplateRepository
                .GetByTemplateIdAsync(tier.DesignTemplateId, activeOnly: true);

            var candidateNurseries = nurseryTemplateMappings
                .Where(x => x.Nursery != null && x.Nursery.IsActive == true)
                .Select(x => x.Nursery!)
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();

            if (request.NurseryId.HasValue)
            {
                var preferredNursery = await _unitOfWork.NurseryRepository.GetByIdAsync(request.NurseryId.Value)
                    ?? throw new NotFoundException($"Preferred nursery {request.NurseryId.Value} not found");

                if (preferredNursery.IsActive != true)
                    throw new BadRequestException("Preferred nursery is inactive");

                candidateNurseries = candidateNurseries
                    .Where(x => x.Id == request.NurseryId.Value)
                    .ToList();

                if (!candidateNurseries.Any())
                    throw new BadRequestException("Preferred nursery does not offer this design template");
            }

            if (!candidateNurseries.Any())
                throw new BadRequestException("No nursery is available for the selected design template at the moment");

            var requiredSpecIds = (await _unitOfWork.DesignTemplateSpecializationRepository
                    .GetByTemplateIdAsync(tier.DesignTemplateId))
                .Select(x => x.SpecializationId)
                .ToHashSet();

            var selectedNursery = await SelectBestNurseryAsync(
                candidateNurseries,
                requiredSpecIds,
                request.Latitude,
                request.Longitude);

            if (selectedNursery == null)
            {
                selectedNursery = await SelectFallbackNurseryAsync(
                    candidateNurseries,
                    request.Latitude,
                    request.Longitude);

                if (selectedNursery == null)
                    throw new BadRequestException("No nursery is available for the selected design template at the moment");
            }

            var totalPrice = tier.PackagePrice;
            var depositAmount = Math.Round(totalPrice * 0.3m, 2, MidpointRounding.AwayFromZero);

            var registration = new DesignRegistration
            {
                UserId = userId,
                NurseryId = selectedNursery.Id,
                DesignTemplateTierId = request.DesignTemplateTierId,
                TotalPrice = totalPrice,
                DepositAmount = depositAmount,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                CurrentStateImageUrl = null,
                Address = request.Address.Trim(),
                Phone = request.Phone.Trim(),
                CustomerNote = string.IsNullOrWhiteSpace(request.CustomerNote) ? null : request.CustomerNote.Trim(),
                CancelReason = BuildCancelReasonWithRouteMeta(
                    new HashSet<int>(),
                    request.NurseryId.HasValue,
                    null),
                Status = (int)DesignRegistrationStatus.PendingApproval,
                CreatedAt = DateTime.Now
            };

            _unitOfWork.DesignRegistrationRepository.PrepareCreate(registration);
            await _unitOfWork.SaveAsync();

            var created = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(registration.Id)
                ?? throw new NotFoundException($"DesignRegistration {registration.Id} not found after create");

            return created.ToResponse();
        }

        public async Task<PaginatedResult<DesignRegistrationResponseDto>> GetMyRegistrationsAsync(int userId, Pagination pagination, int? status = null)
        {
            var result = await _unitOfWork.DesignRegistrationRepository.GetByUserIdAsync(userId, pagination, status);
            return new PaginatedResult<DesignRegistrationResponseDto>(
                result.Items.Select(x => x.ToResponse()).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<PaginatedResult<DesignRegistrationResponseDto>> GetByAssignedCaretakerAsync(int caretakerId, Pagination pagination)
        {
            // Only return registrations with status InProgress or AwaitFinalPayment for caretakers
            var statuses = new List<int> { (int)DesignRegistrationStatus.InProgress, 
                                           (int)DesignRegistrationStatus.AwaitFinalPayment, 
                                           (int)DesignRegistrationStatus.Completed, 
                                           (int)DesignRegistrationStatus.Cancelled };
            var result = await _unitOfWork.DesignRegistrationRepository.GetByAssignedCaretakerIdWithStatusesAsync(caretakerId, statuses, pagination);
            return new PaginatedResult<DesignRegistrationResponseDto>(
                result.Items.Select(x => x.ToResponse()).ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<PaginatedResult<DesignRegistrationResponseDto>> GetPendingForNurseryAsync(int managerId, Pagination pagination)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);
            var result = await _unitOfWork.DesignRegistrationRepository.GetPendingByNurseryIdAsync(nursery.Id, pagination);

            return new PaginatedResult<DesignRegistrationResponseDto>(
                result.Items.Select(x => x.ToResponse()).ToList(),
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

            return registration.ToResponse();
        }

        public async Task<PaginatedResult<DesignRegistrationResponseDto>> GetAllForNurseryAsync(int managerId, Pagination pagination, int? status = null)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);
            var result = await _unitOfWork.DesignRegistrationRepository.GetByNurseryIdAsync(nursery.Id, pagination, status);

            return new PaginatedResult<DesignRegistrationResponseDto>(
                result.Items.Select(x => x.ToResponse()).ToList(),
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

            return registration.ToResponse();
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

            return updated.ToResponse();
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

            var (routeMeta, _) = ParseRejectRouteMeta(registrationDetail.CancelReason);
            var rejectedNurseryHistory = routeMeta.RejectedNurseryIds;
            var isPreferredNurseryRequested = routeMeta.IsPreferredNurseryRequested;

            rejectedNurseryHistory.Add(registrationDetail.NurseryId);

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            var shouldTryRematch = !isPreferredNurseryRequested
                                   && registrationDetail.Status == (int)DesignRegistrationStatus.PendingApproval;

            if (shouldTryRematch)
            {
                var candidateNurseries = await BuildCandidateNurseriesForRerouteAsync(registrationDetail.DesignTemplateTierId);
                var nextNursery = await SelectBestNurseryAsync(
                    candidateNurseries.Where(x => !rejectedNurseryHistory.Contains(x.Id)).ToList(),
                    await GetRequiredSpecializationIdsAsync(registrationDetail.DesignTemplateTierId),
                    registrationDetail.Latitude,
                    registrationDetail.Longitude);

                if (nextNursery != null)
                {
                    registration.NurseryId = nextNursery.Id;
                    registration.CancelReason = BuildCancelReasonWithRouteMeta(
                        rejectedNurseryHistory,
                        isPreferredNurseryRequested,
                        null);
                    registration.Status = (int)DesignRegistrationStatus.PendingApproval;

                    _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);
                    await _unitOfWork.SaveAsync();

                    var rerouted = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                        ?? throw new NotFoundException($"DesignRegistration {id} not found after reroute");

                    return rerouted.ToResponse();
                }
            }

            var normalizedRejectReason = string.IsNullOrWhiteSpace(rejectReason)
                ? null
                : rejectReason.Trim();

            if (normalizedRejectReason == null)
            {
                if (isPreferredNurseryRequested)
                {
                    normalizedRejectReason = "Preferred nursery rejected this design registration";
                }
                else
                {
                    normalizedRejectReason = "All available nurseries have rejected or cannot accept this design registration";
                }
            }

            registration.Status = (int)DesignRegistrationStatus.Rejected;
            registration.CancelReason = BuildCancelReasonWithRouteMeta(
                rejectedNurseryHistory,
                isPreferredNurseryRequested,
                normalizedRejectReason);

            _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);
            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found after reject");

            return updated.ToResponse();
        }

        public async Task<List<StaffWithSpecializationsResponseDto>> GetEligibleCaretakersForRegistrationAsync(int managerId, int registrationId)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(registrationId)
                ?? throw new NotFoundException($"DesignRegistration {registrationId} not found");

            if (registration.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            var requiredSpecIds = await GetRequiredSpecializationIdsAsync(registration.DesignTemplateTierId);
            var allCaretakers = await _unitOfWork.UserRepository.GetCaretakersByNurseryIdAsync(nursery.Id);

            IEnumerable<User> eligible = allCaretakers
                .Where(x => x.Status == (int)UserStatusEnum.Active && x.IsVerified);

            if (requiredSpecIds.Count > 0)
            {
                eligible = eligible.Where(x => requiredSpecIds.All(reqId =>
                    x.StaffSpecializations.Any(ss => ss.SpecializationId == reqId)));
            }

            var eligibleList = eligible
                .OrderBy(x => x.Username ?? string.Empty)
                .ThenBy(x => x.Id)
                .ToList();

            if (!eligibleList.Any())
                return new List<StaffWithSpecializationsResponseDto>();

            var workloads = await _unitOfWork.DesignRegistrationRepository.CountOpenAssignmentsByCaretakerIdsAsync(
                eligibleList.Select(x => x.Id).ToList(),
                new List<int> { nursery.Id });

            return eligibleList
                .Where(x => x.Id == registration.AssignedCaretakerId
                            || (workloads.TryGetValue(x.Id, out var count) ? count : 0) == 0)
                .OrderBy(x => workloads.TryGetValue(x.Id, out var count) ? count : 0)
                .ThenBy(x => x.Username ?? string.Empty)
                .ThenBy(x => x.Id)
                .Select(NurseryService.MapToStaffDtoPublic)
                .ToList();
        }

        public async Task<List<EligibleCaretakerWithAvailabilityDto>> GetEligibleCaretakersForRegistrationWithAvailabilityAsync(int managerId, int registrationId, DateOnly? startDate = null)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(registrationId)
                ?? throw new NotFoundException($"DesignRegistration {registrationId} not found");

            if (registration.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            var requiredSpecIds = await GetRequiredSpecializationIdsAsync(registration.DesignTemplateTierId);
            var allCaretakers = await _unitOfWork.UserRepository.GetCaretakersByNurseryIdAsync(nursery.Id);

            IEnumerable<User> eligible = allCaretakers
                .Where(x => x.Status == (int)UserStatusEnum.Active && x.IsVerified);

            if (requiredSpecIds.Count > 0)
            {
                eligible = eligible.Where(x => requiredSpecIds.All(reqId =>
                    x.StaffSpecializations.Any(ss => ss.SpecializationId == reqId)));
            }

            var eligibleList = eligible
                .OrderBy(x => x.Username ?? string.Empty)
                .ThenBy(x => x.Id)
                .ToList();

            if (!eligibleList.Any())
                return new List<EligibleCaretakerWithAvailabilityDto>();

            var workloads = await _unitOfWork.DesignRegistrationRepository.CountOpenAssignmentsByCaretakerIdsAsync(
                eligibleList.Select(x => x.Id).ToList(),
                new List<int> { nursery.Id });

            // Get start date and estimated days for schedule conflict check
            var tasks = await _unitOfWork.DesignTaskRepository.GetByRegistrationIdAsync(registrationId);
            var firstTask = tasks.OrderBy(x => x.ScheduledDate ?? DateOnly.MaxValue).FirstOrDefault();
            var conflictStartDate = startDate ?? firstTask?.ScheduledDate;
            var estimatedDays = registration.DesignTemplateTier?.EstimatedDays ?? 1;
            if (estimatedDays <= 0) estimatedDays = 1;

            var result = new List<EligibleCaretakerWithAvailabilityDto>();

            foreach (var caretaker in eligibleList)
            {
                var dto = new EligibleCaretakerWithAvailabilityDto
                {
                    Staff = NurseryService.MapToStaffDtoPublic(caretaker)
                };

                // Check schedule conflicts only if there's a start date
                if (conflictStartDate.HasValue)
                {
                    var (isAvailable, conflicts) = await CheckCaretakerScheduleConflictAsync(
                        caretaker.Id, conflictStartDate.Value, estimatedDays, registrationId);

                    dto.IsAvailable = isAvailable;
                    if (conflicts.Any())
                    {
                        dto.ConflictDates = conflicts
                            .Select(x => x.ScheduledDate?.ToString("dd/MM/yyyy") ?? "N/A")
                            .Distinct()
                            .ToList();
                    }
                }
                else
                {
                    // No scheduled date yet, consider as available
                    dto.IsAvailable = true;
                }

                result.Add(dto);
            }

            // Sort: available first, then by workload, then by name
            return result
                .OrderByDescending(x => x.IsAvailable)
                .ThenBy(x => workloads.TryGetValue(x.Staff.Id, out var count) ? count : 0)
                .ThenBy(x => x.Staff.Username ?? string.Empty)
                .ToList();
        }

        private async Task<(bool IsAvailable, List<DesignTask> Conflicts)> CheckCaretakerScheduleConflictAsync(
            int caretakerId, DateOnly startDate, int estimatedDays, int registrationId)
        {
            var dates = BuildConsecutiveWorkingDates(startDate, estimatedDays);
            var conflicts = new List<DesignTask>();
            var pagination = new Pagination { PageNumber = 1, PageSize = 1000 };

            foreach (var date in dates)
            {
                var tasksOnDate = await _unitOfWork.DesignTaskRepository
                    .GetByAssignedStaffIdAsync(caretakerId, pagination, status: null, from: date, to: date);

                foreach (var task in tasksOnDate.Items)
                {
                    // Skip cancelled tasks
                    if (task.Status == (int)DesignTaskStatusEnum.Cancelled)
                        continue;

                    // Skip tasks from the registration being assigned
                    if (task.DesignRegistrationId == registrationId)
                        continue;

                    conflicts.Add(task);
                }

                if (conflicts.Any())
                    return (false, conflicts);
            }

            return (true, conflicts);
        }

        public async Task<DesignRegistrationResponseDto> AssignCaretakerAsync(int managerId, int id, int caretakerId, DateOnly? startDate = null)
        {
            var nursery = await ResolveOperatorNurseryAsync(managerId);

            var registrationDetail = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (registrationDetail.NurseryId != nursery.Id)
                throw new ForbiddenException("This registration does not belong to your nursery");

            if (registrationDetail.Status != (int)DesignRegistrationStatus.DepositPaid
                && registrationDetail.Status != (int)DesignRegistrationStatus.InProgress)
                throw new BadRequestException("Can only assign caretaker when registration is DepositPaid, or InProgress");

            var caretaker = await _unitOfWork.UserRepository.GetCaretakerByIdWithSpecializationsAsync(caretakerId, nursery.Id)
                ?? throw new NotFoundException($"Caretaker {caretakerId} not found in your nursery");

            if (caretaker.Status != (int)UserStatusEnum.Active || !caretaker.IsVerified)
                throw new BadRequestException("Caretaker account is not active or verified");

            var requiredSpecIds = await GetRequiredSpecializationIdsAsync(registrationDetail.DesignTemplateTierId);
            if (requiredSpecIds.Count > 0 &&
                !requiredSpecIds.All(reqId => caretaker.StaffSpecializations.Any(ss => ss.SpecializationId == reqId)))
            {
                throw new BadRequestException("Selected caretaker does not have all required specializations for this design registration");
            }

            if (registrationDetail.AssignedCaretakerId != caretakerId)
            {
                var workloads = await _unitOfWork.DesignRegistrationRepository
                    .CountOpenAssignmentsByCaretakerIdsAsync(new List<int> { caretakerId }, new List<int> { nursery.Id });
                var currentOpenAssignments = workloads.TryGetValue(caretakerId, out var count) ? count : 0;
                if (currentOpenAssignments > 0)
                {
                    throw new BadRequestException("Selected caretaker is currently handling another open design registration");
                }

                // Check schedule conflicts
                var tasks = await _unitOfWork.DesignTaskRepository.GetByRegistrationIdAsync(id);
                var firstTask = tasks.OrderBy(x => x.ScheduledDate ?? DateOnly.MaxValue).FirstOrDefault();
                var conflictStartDate = startDate ?? firstTask?.ScheduledDate;

                if (!conflictStartDate.HasValue)
                    throw new BadRequestException("StartDate is required to schedule tasks for this registration");

                if (conflictStartDate.HasValue)
                {
                    var estimatedDays = registrationDetail.DesignTemplateTier?.EstimatedDays ?? 1;
                    if (estimatedDays <= 0) estimatedDays = 1;

                    var (isAvailable, conflicts) = await CheckCaretakerScheduleConflictAsync(
                        caretakerId, conflictStartDate.Value, estimatedDays, id);

                    if (!isAvailable && conflicts.Any())
                    {
                        var conflictDates = string.Join(", ", conflicts.Select(x => x.ScheduledDate?.ToString("dd/MM/yyyy") ?? "N/A").Distinct());
                        throw new BadRequestException($"Selected caretaker has conflicting tasks on: {conflictDates}");
                    }
                }
            }

            var registration = await _unitOfWork.DesignRegistrationRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            registration.AssignedCaretakerId = caretakerId;
            if (registration.Status == (int)DesignRegistrationStatus.DepositPaid)
            {
                registration.Status = (int)DesignRegistrationStatus.InProgress;
            }
            _unitOfWork.DesignRegistrationRepository.PrepareUpdate(registration);

            var registrationTasks = await _unitOfWork.DesignTaskRepository.GetByRegistrationIdAsync(id);
            var activeTasks = registrationTasks
                .Where(t => t.Status != (int)DesignTaskStatusEnum.Completed && t.Status != (int)DesignTaskStatusEnum.Cancelled)
                .OrderBy(t => t.ScheduledDate ?? DateOnly.MaxValue)
                .ThenBy(t => t.Id)
                .ToList();

            if (startDate.HasValue)
            {
                var scheduleDates = BuildConsecutiveWorkingDates(startDate.Value, activeTasks.Count);
                for (var i = 0; i < activeTasks.Count; i++)
                {
                    var task = activeTasks[i];
                    var trackedTask = await _unitOfWork.DesignTaskRepository.GetByIdAsync(task.Id);
                    if (trackedTask == null)
                        continue;

                    trackedTask.ScheduledDate = scheduleDates[i];
                    trackedTask.AssignedStaffId = caretakerId;

                    if (trackedTask.Status == (int)DesignTaskStatusEnum.Pending)
                    {
                        trackedTask.Status = (int)DesignTaskStatusEnum.Assigned;
                    }

                    _unitOfWork.DesignTaskRepository.PrepareUpdate(trackedTask);
                }
            }
            else
            {
                if (activeTasks.Any(t => !t.ScheduledDate.HasValue))
                    throw new BadRequestException("StartDate is required to schedule tasks for this registration");

                foreach (var task in activeTasks)
                {
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
            }

            await _unitOfWork.SaveAsync();

            var updated = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found after assign caretaker");

            return updated.ToResponse();
        }

        public async Task<DesignRegistrationResponseDto> UpdateSurveyInfoAsync(int caretakerId, int id, UpdateDesignRegistrationSurveyInfoRequestDto request, IFormFile? currentStateImage = null)
        {
            if (request == null)
                throw new BadRequestException("Request body is required");

            if (!request.Width.HasValue && !request.Length.HasValue
                && currentStateImage == null)
                throw new BadRequestException("At least one survey field or current-state image must be provided");

            var registrationDetail = await _unitOfWork.DesignRegistrationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignRegistration {id} not found");

            if (registrationDetail.AssignedCaretakerId != caretakerId)
                throw new ForbiddenException("Only assigned caretaker can update survey info");

            if (registrationDetail.Status != (int)DesignRegistrationStatus.InProgress)
                throw new BadRequestException("Survey info can only be updated when registration is InProgress");

            if (request.Width.HasValue != request.Length.HasValue)
                throw new BadRequestException("Width and Length must be provided together");

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

            return updated.ToResponse();
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

            return updated.ToResponse();
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

            return updated.ToResponse();
        }

        private async Task<Nursery?> SelectBestNurseryAsync(
            List<Nursery> candidateNurseries,
            HashSet<int> requiredSpecIds,
            decimal? latitude,
            decimal? longitude)
        {
            if (candidateNurseries == null || candidateNurseries.Count == 0)
            {
                return null;
            }

            var nurseryIds = candidateNurseries.Select(x => x.Id).Distinct().ToList();
            var openWorkloads = await _unitOfWork.DesignRegistrationRepository.CountOpenByNurseryIdsAsync(nurseryIds);
            var useDistance = latitude.HasValue && longitude.HasValue;

            var scoredCandidates = new List<(Nursery Nursery, int EligibleCount, int Workload, double DistanceKm)>();

            foreach (var nursery in candidateNurseries)
            {
                var staffs = await _unitOfWork.UserRepository.GetStaffAndCaretakersByNurseryIdAsync(nursery.Id);

                IEnumerable<User> eligible = staffs
                    .Where(u => u.Status == (int)UserStatusEnum.Active && u.IsVerified);

                if (requiredSpecIds.Count > 0)
                {
                    eligible = eligible.Where(u => requiredSpecIds.All(reqId =>
                        u.StaffSpecializations.Any(ss => ss.SpecializationId == reqId)));
                }

                var eligibleCount = eligible.Count();
                if (eligibleCount == 0)
                {
                    continue;
                }

                var workload = openWorkloads.TryGetValue(nursery.Id, out var count) ? count : 0;
                var distanceKm = useDistance && nursery.Latitude.HasValue && nursery.Longitude.HasValue
                    ? HaversineKm(latitude!.Value, longitude!.Value, nursery.Latitude.Value, nursery.Longitude.Value)
                    : double.MaxValue;

                scoredCandidates.Add((nursery, eligibleCount, workload, distanceKm));
            }

            return scoredCandidates
                .OrderByDescending(x => x.EligibleCount)
                .ThenBy(x => x.Workload)
                .ThenBy(x => x.DistanceKm)
                .ThenBy(x => x.Nursery.Id)
                .Select(x => x.Nursery)
                .FirstOrDefault();
        }

        private async Task<HashSet<int>> GetRequiredSpecializationIdsAsync(int designTemplateTierId)
        {
            var tier = await _unitOfWork.DesignTemplateTierRepository.GetByIdAsync(designTemplateTierId)
                ?? throw new NotFoundException($"DesignTemplateTier {designTemplateTierId} not found");

            return (await _unitOfWork.DesignTemplateSpecializationRepository
                    .GetByTemplateIdAsync(tier.DesignTemplateId))
                .Select(x => x.SpecializationId)
                .ToHashSet();
        }

        private async Task<List<Nursery>> BuildCandidateNurseriesForRerouteAsync(int designTemplateTierId)
        {
            var tier = await _unitOfWork.DesignTemplateTierRepository.GetByIdAsync(designTemplateTierId)
                ?? throw new NotFoundException($"DesignTemplateTier {designTemplateTierId} not found");

            var nurseryTemplateMappings = await _unitOfWork.NurseryDesignTemplateRepository
                .GetByTemplateIdAsync(tier.DesignTemplateId, activeOnly: true);

            return nurseryTemplateMappings
                .Where(x => x.Nursery != null && x.Nursery.IsActive == true)
                .Select(x => x.Nursery!)
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();
        }

        private async Task<Nursery?> SelectFallbackNurseryAsync(
            List<Nursery> candidateNurseries,
            decimal? latitude,
            decimal? longitude)
        {
            if (candidateNurseries == null || candidateNurseries.Count == 0)
            {
                return null;
            }

            var nurseryIds = candidateNurseries.Select(x => x.Id).Distinct().ToList();
            var openWorkloads = await _unitOfWork.DesignRegistrationRepository.CountOpenByNurseryIdsAsync(nurseryIds);
            var useDistance = latitude.HasValue && longitude.HasValue;

            return candidateNurseries
                .Select(nursery => new
                {
                    Nursery = nursery,
                    Workload = openWorkloads.TryGetValue(nursery.Id, out var count) ? count : 0,
                    DistanceKm = useDistance && nursery.Latitude.HasValue && nursery.Longitude.HasValue
                        ? HaversineKm(latitude!.Value, longitude!.Value, nursery.Latitude.Value, nursery.Longitude.Value)
                        : double.MaxValue
                })
                .OrderBy(x => x.Workload)
                .ThenBy(x => x.DistanceKm)
                .ThenBy(x => x.Nursery.Id)
                .Select(x => x.Nursery)
                .FirstOrDefault();
        }

        private static double HaversineKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double earthRadiusKm = 6371.0;

            var dLat = (double)(lat2 - lat1) * Math.PI / 180.0;
            var dLon = (double)(lon2 - lon1) * Math.PI / 180.0;
            var originLat = (double)lat1 * Math.PI / 180.0;
            var targetLat = (double)lat2 * Math.PI / 180.0;

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
                            .Where(x => x > 0)
                            .ToHashSet(),
                        IsPreferredNurseryRequested = parsedPayload.IsPreferredNurseryRequested == true
                    }, userReason);
                }
            }
            catch
            {
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<List<int>>(jsonPart) ?? new List<int>();
                return (new RejectRouteMeta
                {
                    RejectedNurseryIds = parsed.Where(x => x > 0).ToHashSet(),
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
                RejectedNurseryIds = rejectedNurseryHistory.OrderBy(x => x).ToList(),
                IsPreferredNurseryRequested = isPreferredNurseryRequested ? true : null
            };
            var historyJson = JsonSerializer.Serialize(payload);

            return normalizedReason == null
                ? $"{RejectRouteMetaPrefix}{historyJson}"
                : $"{RejectRouteMetaPrefix}{historyJson}|{normalizedReason}";
        }

        private static List<DateOnly> BuildConsecutiveWorkingDates(DateOnly startDate, int totalDays)
        {
            var dates = new List<DateOnly>();
            var cursor = startDate;

            while (dates.Count < totalDays)
            {
                if (cursor.DayOfWeek != DayOfWeek.Sunday)
                {
                    dates.Add(cursor);
                }

                cursor = cursor.AddDays(1);
            }

            return dates;
        }

    }
}
