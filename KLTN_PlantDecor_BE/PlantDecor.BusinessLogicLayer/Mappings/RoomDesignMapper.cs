using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class RoomDesignMapper
    {
        public static RoomAnalysisDto ToRoomAnalysisDto(this RoomAnalysisJsonDto source, Func<string?, string> mapRoomType)
        {
            return new RoomAnalysisDto
            {
                RoomType = mapRoomType(source.RoomType),
                RoomSize = source.RoomSize ?? "medium",
                LightingCondition = source.LightingCondition ?? "medium",
                InteriorStyle = source.InteriorStyle ?? "modern",
                AvailableSpace = source.AvailableSpace ?? "floor",
                ColorPalette = source.ColorPalette ?? new List<string>(),
                Summary = source.Summary ?? "Không có thông tin phân tích"
            };
        }

        public static EmbeddingSearchItemDto ToEmbeddingSearchItem(this Embedding embedding)
        {
            return new EmbeddingSearchItemDto
            {
                EntityType = embedding.EntityType,
                Metadata = embedding.Metadata
            };
        }
    }
}
