using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;

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
                RoomImageId = layout.RoomImageId,
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
                LayoutDesignAiResponseImages = layout.LayoutDesignAiResponseImages
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
                CreatedAt = image.CreatedAt
            };
        }

        public static List<LayoutDesignListResponseDto> ToLayoutDesignListResponseList(this IEnumerable<LayoutDesign> layouts)
        {
            return layouts.Select(layout => layout.ToLayoutDesignListResponse()).ToList();
        }
    }
}
