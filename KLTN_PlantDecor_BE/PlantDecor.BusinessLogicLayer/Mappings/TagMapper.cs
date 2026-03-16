using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class TagMapper
    {
        #region Entity to Response
        public static TagResponseDto ToResponse(this Tag tag)
        {
            if (tag == null) return null!;
            return new TagResponseDto
            {
                Id = tag.Id,
                TagName = tag.TagName,
                TagType = tag.TagType,
                TagTypeName = ((DataAccessLayer.Enums.TagTypeEnum)tag.TagType).ToString()
            };
        }

        public static List<TagResponseDto> ToResponseList(this IEnumerable<Tag> tags)
        {
            return tags.Select(t => t.ToResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static Tag ToEntity(this TagRequestDto request)
        {
            if (request == null) return null!;

            return new Tag
            {
                TagName = request.TagName,
                TagType = request.TagType
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this TagUpdateDto request, Tag tag)
        {
            if (request == null || tag == null) return;

            if (request.TagName != null)
                tag.TagName = request.TagName;

            if (request.TagType.HasValue)
                tag.TagType = request.TagType.Value;
        }
        #endregion
    }
}
