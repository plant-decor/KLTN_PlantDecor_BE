using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class LayoutDesignGeneratedImageMapper
    {
        public static LayoutDesignGeneratedImageDto ToLayoutDesignGeneratedImageDto(this LayoutDesignAiResponseImage image)
        {
            if (image == null) return null!;

            return new LayoutDesignGeneratedImageDto
            {
                Id = image.Id,
                LayoutDesignId = image.LayoutDesignId,
                LayoutDesignPlantId = image.LayoutDesignPlantId,
                CommonPlantId = image.LayoutDesignPlant?.CommonPlantId,
                PlantInstanceId = image.LayoutDesignPlant?.PlantInstanceId,
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