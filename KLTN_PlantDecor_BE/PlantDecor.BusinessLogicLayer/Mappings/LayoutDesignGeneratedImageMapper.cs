using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class LayoutDesignGeneratedImageMapper
    {
        public static LayoutDesignGeneratedImageDto ToLayoutDesignGeneratedImageDto(this LayoutDesignAiResponseImage image)
        {
            if (image == null) return null!;

            var commonPlant = image.LayoutDesignPlant?.CommonPlant;
            var plantInstance = image.LayoutDesignPlant?.PlantInstance;

            return new LayoutDesignGeneratedImageDto
            {
                Id = image.Id,
                LayoutDesignId = image.LayoutDesignId,
                LayoutDesignPlantId = image.LayoutDesignPlantId,
                CommonPlantId = image.LayoutDesignPlant?.CommonPlantId,
                PlantInstanceId = image.LayoutDesignPlant?.PlantInstanceId,
                Name = plantInstance?.Plant?.Name ?? commonPlant?.Plant?.Name,
                Price = plantInstance?.SpecificPrice ?? commonPlant?.Plant?.BasePrice,
                ImageUrl = image.ImageUrl,
                FluxPromptUsed = image.FluxPromptUsed,
                CreatedAt = image.CreatedAt
            };
        }

        public static List<LayoutDesignGeneratedImageDto> ToLayoutDesignGeneratedImageDtoList(this IEnumerable<LayoutDesignAiResponseImage> images)
        {
            return images.Select(image => image.ToLayoutDesignGeneratedImageDto()).ToList();
        }
    }
}