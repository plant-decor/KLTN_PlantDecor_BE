using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class LayoutDesignMapper
    {
        public static LayoutDesignListResponseDto ToLayoutDesignListResponse(this LayoutDesign layout)
        {
            if (layout == null) return null!;

            return new LayoutDesignListResponseDto
            {
                Id = layout.Id,
                UserId = layout.UserId,
                RoomImageId = layout.LayoutDesignRoomImages
                    .OrderBy(link => link.OrderIndex ?? int.MaxValue)
                    .ThenBy(link => link.RoomImageId)
                    .Select(link => link.RoomImageId)
                    .FirstOrDefault(),
                PreviewImageUrl = layout.PreviewImageUrl,
                RawResponse = layout.RawResponse,
                Status = layout.Status,
                IsSaved = layout.IsSaved,
                CreatedAt = layout.CreatedAt,
                LayoutDesignPlants = layout.LayoutDesignPlants
                    .OrderByDescending(plant => plant.CreatedAt)
                    .ThenByDescending(plant => plant.Id)
                    .Select(plant => plant.ToLayoutDesignPlantResponse())
                    .ToList(),
                LayoutDesignAiResponseImages = FilterImagesForCustomer(layout.LayoutDesignAiResponseImages)
                    .OrderByDescending(image => image.CreatedAt)
                    .ThenByDescending(image => image.Id)
                    .Select(image => image.ToLayoutDesignAiResponseImageResponse())
                    .ToList()
            };
        }

        public static LayoutDesignPlantResponseDto ToLayoutDesignPlantResponse(this LayoutDesignPlant plant)
        {
            if (plant == null) return null!;

            return new LayoutDesignPlantResponseDto
            {
                Id = plant.Id,
                LayoutDesignId = plant.LayoutDesignId,
                CommonPlantId = plant.CommonPlantId,
                PlantInstanceId = plant.PlantInstanceId,
                PlantReason = plant.PlantReason,
                PlacementPosition = plant.PlacementPosition,
                PlacementReason = plant.PlacementReason,
                CreatedAt = plant.CreatedAt
            };
        }

        public static LayoutDesignAiResponseImageResponseDto ToLayoutDesignAiResponseImageResponse(this LayoutDesignAiResponseImage image)
        {
            if (image == null) return null!;

            return new LayoutDesignAiResponseImageResponseDto
            {
                Id = image.Id,
                LayoutDesignId = image.LayoutDesignId,
                LayoutDesignPlantId = image.LayoutDesignPlantId,
                ImageUrl = image.ImageUrl,
                PublicId = image.PublicId,
                FluxPromptUsed = image.FluxPromptUsed,
                SourceType = image.SourceType.HasValue
                    ? (LayoutDesignImageSourceTypeEnum)image.SourceType.Value
                    : null,
                ReplacesImageId = image.ReplacesImageId,
                CreatedAt = image.CreatedAt
            };
        }

        private static List<LayoutDesignAiResponseImage> FilterImagesForCustomer(IEnumerable<LayoutDesignAiResponseImage> images)
        {
            if (images == null)
            {
                return new List<LayoutDesignAiResponseImage>();
            }

            var materialized = images.ToList();
            if (materialized.Count == 0)
            {
                return materialized;
            }

            var manualByPlant = materialized
                .Where(image => IsManualImage(image)
                    && image.LayoutDesignPlantId.HasValue
                    && !string.IsNullOrWhiteSpace(image.ImageUrl))
                .GroupBy(image => image.LayoutDesignPlantId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(image => image.CreatedAt ?? DateTime.MinValue)
                        .ThenByDescending(image => image.Id)
                        .First());

            var aiByPlant = materialized
                .Where(image => !IsManualImage(image) && image.LayoutDesignPlantId.HasValue)
                .GroupBy(image => image.LayoutDesignPlantId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(image => image.CreatedAt ?? DateTime.MinValue)
                        .ThenByDescending(image => image.Id)
                        .First());

            var selected = new List<LayoutDesignAiResponseImage>();
            foreach (var plantId in manualByPlant.Keys.Union(aiByPlant.Keys))
            {
                if (manualByPlant.TryGetValue(plantId, out var manual))
                {
                    selected.Add(manual);
                    continue;
                }

                if (aiByPlant.TryGetValue(plantId, out var ai))
                {
                    selected.Add(ai);
                }
            }

            var manualComposite = materialized
                .Where(image => IsManualImage(image)
                    && !image.LayoutDesignPlantId.HasValue
                    && !string.IsNullOrWhiteSpace(image.ImageUrl))
                .OrderByDescending(image => image.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(image => image.Id)
                .FirstOrDefault();

            if (manualComposite != null)
            {
                selected.Add(manualComposite);
            }

            return selected;
        }

        private static bool IsManualImage(LayoutDesignAiResponseImage image)
        {
            return image.SourceType.HasValue &&
                   image.SourceType.Value == (int)LayoutDesignImageSourceTypeEnum.Manual;
        }

        public static List<LayoutDesignListResponseDto> ToLayoutDesignListResponseList(this IEnumerable<LayoutDesign> layouts)
        {
            return layouts.Select(layout => layout.ToLayoutDesignListResponse()).ToList();
        }
    }
}
