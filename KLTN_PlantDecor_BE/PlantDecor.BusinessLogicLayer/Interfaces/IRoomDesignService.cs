using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IRoomDesignService
    {
        /// <summary>
        /// Get paginated layout designs of the authenticated user with plants and AI response images
        /// </summary>
        /// <param name="userId">Authenticated user id</param>
        /// <param name="pagination">Pagination settings</param>
        /// <returns>Paginated list of layouts</returns>
        Task<PaginatedResult<LayoutDesignListResponseDto>> GetAllLayoutsAsync(int userId, Pagination pagination);

        /// <summary>
        /// Analyze room image and recommend suitable plants from database
        /// </summary>
        /// <param name="request">Room design request with image and filters</param>
        /// <returns>Room analysis and plant recommendations</returns>
        Task<RoomDesignResponseDto> AnalyzeAndRecommendAsync(
            RoomDesignRequestDto request,
            bool inferNaturalLightFromAi = false);

        /// <summary>
        /// Analyze existing uploaded room images and persist design artifacts
        /// </summary>
        /// <param name="request">Room image ids and optional filters/preferences</param>
        /// <param name="userId">Authenticated user id</param>
        /// <returns>Room analysis and plant recommendations</returns>
        Task<RoomDesignResponseDto> AnalyzeAndRecommendByRoomImagesAsync(
            AnalyzeAndRecommendByRoomImagesRequest request,
            int userId);

        /// <summary>
        /// Analyze room image only (without recommendations)
        /// </summary>
        /// <param name="imageBase64">Base64 encoded image</param>
        /// <returns>Room analysis result</returns>
        Task<RoomAnalysisDto> AnalyzeRoomAsync(string imageBase64);

        /// <summary>
        /// Get active Plant options for allergy selection
        /// </summary>
        /// <param name="keyword">Optional keyword to filter by plant name</param>
        /// <param name="take">Maximum number of options to return</param>
        /// <returns>List of active plants for allergy multi-select</returns>
        Task<List<AllergyPlantOptionDto>> GetAllergyPlantOptionsAsync(string? keyword = null, int take = 50);
    }
}
