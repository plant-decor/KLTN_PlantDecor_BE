using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class ServiceRegistrationMapper
    {
        private const string RejectRouteMetaPrefix = "__route_meta__:";

        public static ServiceRegistrationResponseDto ToResponse(this ServiceRegistration registration)
        {
            return new ServiceRegistrationResponseDto
            {
                Id = registration.Id,
                Status = registration.Status,
                StatusName = registration.Status.HasValue ? ((ServiceRegistrationStatusEnum)registration.Status.Value).ToString() : null,
                ServiceDate = registration.ServiceDate,
                TotalSessions = registration.TotalSessions,
                Address = registration.Address,
                Phone = registration.Phone,
                Note = registration.Note,
                Latitude = registration.Latitude,
                Longitude = registration.Longitude,
                ScheduleDaysOfWeek = registration.ScheduleDaysOfWeek,
                CancelReason = ResolveDisplayCancelReason(registration.Status, registration.CancelReason),
                CreatedAt = registration.CreatedAt,
                ApprovedAt = registration.ApprovedAt,
                OrderId = registration.OrderId,
                NurseryCareService = registration.NurseryCareService == null ? null : new NurseryCareServiceSummaryDto
                {
                    Id = registration.NurseryCareService.Id,
                    NurseryId = registration.NurseryCareService.NurseryId,
                    NurseryName = registration.NurseryCareService.Nursery?.Name,
                    CareServicePackage = registration.NurseryCareService.CareServicePackage == null ? null : new CareServicePackageSummaryDto
                    {
                        Id = registration.NurseryCareService.CareServicePackage.Id,
                        Name = registration.NurseryCareService.CareServicePackage.Name,
                        Description = registration.NurseryCareService.CareServicePackage.Description,
                        VisitPerWeek = registration.NurseryCareService.CareServicePackage.VisitPerWeek,
                        DurationDays = registration.NurseryCareService.CareServicePackage.DurationDays,
                        ServiceType = registration.NurseryCareService.CareServicePackage.ServiceType,
                        UnitPrice = registration.NurseryCareService.CareServicePackage.UnitPrice,
                    }
                },
                PrefferedShift = registration.PrefferedShift == null ? null : new ShiftSummaryDto
                {
                    Id = registration.PrefferedShift.Id,
                    ShiftName = registration.PrefferedShift.ShiftName,
                    StartTime = registration.PrefferedShift.StartTime,
                    EndTime = registration.PrefferedShift.EndTime
                },
                Customer = registration.User == null ? null : registration.User.ToUserSummary(),
                MainCaretaker = registration.MainCaretaker == null ? null : registration.MainCaretaker.ToUserSummary(),
                CurrentCaretaker = registration.CurrentCaretaker == null ? null : registration.CurrentCaretaker.ToUserSummary(),
                Progresses = registration.ServiceProgresses
                    .OrderBy(sp => sp.TaskDate)
                    .Select(ServiceProgressService.MapToDto)
                    .ToList(),
                Rating = registration.ServiceRating == null ? null : ServiceRatingService.MapToDto(registration.ServiceRating)
            };
        }

        public static UserSummaryDto ToUserSummary(this User user) => new()
        {
            Id = user.Id,
            FullName = user.Username,
            Email = user.Email,
            Phone = user.PhoneNumber,
            Avatar = user.AvatarUrl
        };

        private static string? ResolveDisplayCancelReason(int? status, string? storedCancelReason)
        {
            if (status != (int)ServiceRegistrationStatusEnum.Rejected &&
                status != (int)ServiceRegistrationStatusEnum.Cancelled)
            {
                return null;
            }

            return ExtractUserReasonFromStoredCancelReason(storedCancelReason);
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
    }
}