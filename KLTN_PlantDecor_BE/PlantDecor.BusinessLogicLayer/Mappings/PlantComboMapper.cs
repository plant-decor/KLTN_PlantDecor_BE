using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class PlantComboMapper
    {
        #region Entity to Response
        public static PlantComboResponseDto ToResponse(this PlantCombo combo)
        {
            if (combo == null) return null!;
            var seasonName = MapSeasonName(combo.Season);
            var comboTypeName = MapComboTypeName(combo.ComboType);

            return new PlantComboResponseDto
            {
                Id = combo.Id,
                ComboCode = combo.ComboCode,
                ComboName = combo.ComboName,
                ComboType = combo.ComboType,
                ComboTypeName = comboTypeName,
                Description = combo.Description,
                SuitableSpace = MapLightRequirementName(combo.SuitableSpace),
                SuitableRooms = FormatRoomTypes(combo.SuitableRooms),
                FengShuiElement = combo.FengShuiElement,
                FengShuiPurpose = combo.FengShuiPurpose,
                PetSafe = combo.PetSafe,
                ChildSafe = combo.ChildSafe,
                ThemeName = combo.ThemeName,
                ThemeDescription = combo.ThemeDescription,
                ComboPrice = combo.ComboPrice,
                Season = combo.Season,
                SeasonName = seasonName,
                IsActive = combo.IsActive,
                ViewCount = combo.ViewCount,
                PurchaseCount = combo.PurchaseCount,
                CreatedAt = combo.CreatedAt,
                UpdatedAt = combo.UpdatedAt,
                ComboItems = combo.PlantComboItems.Select(i => new PlantComboItemResponseDto
                {
                    Id = i.Id,
                    PlantComboId = i.PlantComboId,
                    PlantId = i.PlantId,
                    PlantName = i.Plant?.Name,
                    Quantity = i.Quantity,
                    Notes = i.Notes
                }).ToList(),
                Images = combo.PlantComboImages.Select(i => new PlantComboImageResponseDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary
                }).ToList(),
                TagsNavigation = combo.TagsNavigation.Select(t => new TagResponseDto
                {
                    Id = t.Id,
                    TagName = t.TagName
                }).ToList()
            };
        }

        public static PlantComboListResponseDto ToListResponse(this PlantCombo combo)
        {
            if (combo == null) return null!;
            var seasonName = MapSeasonName(combo.Season);
            var comboTypeName = MapComboTypeName(combo.ComboType);

            return new PlantComboListResponseDto
            {
                Id = combo.Id,
                ComboCode = combo.ComboCode,
                ComboName = combo.ComboName,
                ComboType = combo.ComboType,
                ComboTypeName = comboTypeName,
                ComboPrice = combo.ComboPrice,
                Season = combo.Season,
                SeasonName = seasonName,
                PetSafe = combo.PetSafe,
                ChildSafe = combo.ChildSafe,
                IsActive = combo.IsActive,
                ViewCount = combo.ViewCount,
                PurchaseCount = combo.PurchaseCount,
                PrimaryImageUrl = combo.PlantComboImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl,
                TotalItems = combo.PlantComboItems.Count,
                TagNames = combo.TagsNavigation.Select(t => t.TagName).ToList()
            };
        }

        public static List<PlantComboResponseDto> ToResponseList(this IEnumerable<PlantCombo> combos)
        {
            return combos.Select(c => c.ToResponse()).ToList();
        }

        public static List<PlantComboListResponseDto> ToListResponseList(this IEnumerable<PlantCombo> combos)
        {
            return combos.Select(c => c.ToListResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static PlantCombo ToEntity(this PlantComboRequestDto request)
        {
            if (request == null) return null!;

            return new PlantCombo
            {
                ComboCode = request.ComboCode,
                ComboName = request.ComboName,
                ComboType = request.ComboType,
                Description = request.Description,
                SuitableSpace = request.SuitableSpace,
                SuitableRooms = request.SuitableRooms?.Distinct().ToList(),
                FengShuiElement = request.FengShuiElement,
                FengShuiPurpose = request.FengShuiPurpose,
                ThemeName = request.ThemeName,
                ThemeDescription = request.ThemeDescription,
                ComboPrice = request.ComboPrice,
                Season = request.Season,
                IsActive = request.IsActive,
                ViewCount = 0,
                PurchaseCount = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static PlantComboItem ToComboItemEntity(this PlantComboItemRequestDto request, int comboId)
        {
            if (request == null) return null!;

            return new PlantComboItem
            {
                PlantComboId = comboId,
                PlantId = request.PlantId,
                Quantity = request.Quantity,
                Notes = request.Notes
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this PlantComboUpdateDto request, PlantCombo combo)
        {
            if (request == null || combo == null) return;

            combo.ComboCode = request.ComboCode ?? combo.ComboCode;
            combo.ComboName = request.ComboName ?? combo.ComboName;
            combo.ComboType = request.ComboType ?? combo.ComboType;
            combo.Description = request.Description ?? combo.Description;
            combo.SuitableSpace = request.SuitableSpace ?? combo.SuitableSpace;
            combo.SuitableRooms = request.SuitableRooms != null
                ? request.SuitableRooms.Distinct().ToList()
                : combo.SuitableRooms;
            combo.FengShuiElement = request.FengShuiElement ?? combo.FengShuiElement;
            combo.FengShuiPurpose = request.FengShuiPurpose ?? combo.FengShuiPurpose;
            combo.ThemeName = request.ThemeName ?? combo.ThemeName;
            combo.ThemeDescription = request.ThemeDescription ?? combo.ThemeDescription;
            combo.ComboPrice = request.ComboPrice ?? combo.ComboPrice;
            if (request.Season.HasValue)
            {
                combo.Season = request.Season.Value;
            }
            combo.IsActive = request.IsActive ?? combo.IsActive;
            combo.UpdatedAt = DateTime.Now;
        }
        #endregion

        #region Helper
        private static string? MapSeasonName(int? season)
        {
            if (!season.HasValue || !Enum.IsDefined(typeof(SeasonTypeEnum), season.Value))
            {
                return null;
            }

            return ((SeasonTypeEnum)season.Value).ToString();
        }

        private static string? MapComboTypeName(int? comboType)
        {
            if (!comboType.HasValue || !Enum.IsDefined(typeof(ComboTypeEnum), comboType.Value))
            {
                return null;
            }

            return ((ComboTypeEnum)comboType.Value).ToString();
        }

        private static string? MapLightRequirementName(int? suitableSpace)
        {
            if (!suitableSpace.HasValue || !Enum.IsDefined(typeof(LightRequirementEnum), suitableSpace.Value))
            {
                return null;
            }

            return ((LightRequirementEnum)suitableSpace.Value).ToString();
        }

        private static List<string>? FormatRoomTypes(List<int>? roomTypes)
        {
            return roomTypes?
                .Distinct()
                .Select(MapRoomTypeName)
                .ToList();
        }

        private static string MapRoomTypeName(int roomType)
        {
            if (Enum.IsDefined(typeof(RoomTypeEnum), roomType))
            {
                return ((RoomTypeEnum)roomType).ToString();
            }

            return roomType.ToString();
        }

        public static string GenerateComboCode()
        {
            return $"CMB{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";
        }
        #endregion
    }
}
