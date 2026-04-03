using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IRoomDesignService
    {
        /// <summary>
        /// Analyze room image and recommend suitable plants from database
        /// </summary>
        /// <param name="request">Room design request with image and filters</param>
        /// <returns>Room analysis and plant recommendations</returns>
        Task<RoomDesignResponseDto> AnalyzeAndRecommendAsync(RoomDesignRequestDto request);

        /// <summary>
        /// Analyze room image only (without recommendations)
        /// </summary>
        /// <param name="imageBase64">Base64 encoded image</param>
        /// <returns>Room analysis result</returns>
        Task<RoomAnalysisDto> AnalyzeRoomAsync(string imageBase64);
    }
}
