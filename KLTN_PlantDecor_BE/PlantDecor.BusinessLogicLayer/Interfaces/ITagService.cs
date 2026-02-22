using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ITagService
    {
        Task<List<TagResponseDto>> GetAllTagsAsync();
        Task<TagResponseDto?> GetTagByIdAsync(int id);
        Task<TagResponseDto> CreateTagAsync(TagRequestDto request);
        Task<TagResponseDto> UpdateTagAsync(int id, TagUpdateDto request);
        Task<bool> DeleteTagAsync(int id);
    }
}
