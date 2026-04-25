using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class DesignRegistrationMapper
    {
        public static DesignRegistrationResponseDto ToResponse(this DesignRegistration registration)
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
                Customer = registration.User == null ? null : registration.User.ToUserSummary(),
                AssignedCaretaker = registration.AssignedCaretaker == null ? null : registration.AssignedCaretaker.ToUserSummary(),
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
                    .Select(x => x.ToResponse())
                    .ToList()
            };
        }
    }
}