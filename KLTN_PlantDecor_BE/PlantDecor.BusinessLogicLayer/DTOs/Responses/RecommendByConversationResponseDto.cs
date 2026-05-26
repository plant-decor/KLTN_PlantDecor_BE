using System.Collections.Generic;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class RecommendByConversationResponseDto
    {
        public List<RankedCarePackageDto> RankedPackages { get; set; } = new();
    }
}
