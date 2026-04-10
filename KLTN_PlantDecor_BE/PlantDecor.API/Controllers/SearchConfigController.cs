using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API trả về metadata filter/sort cho các màn search
    /// </summary>
    [Route("api/system/search-config")]
    [ApiController]
    public class SearchConfigController : ControllerBase
    {
        /// <summary>
        /// Metadata cho Plant search
        /// GET /api/system/search-config/plants
        /// </summary>
        [HttpGet("plants")]
        [AllowAnonymous]
        public IActionResult GetPlantSearchConfig()
        {
            var payload = new SearchConfigResponseDto
            {
                FilterEnums = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PlacementType", typeof(PlacementTypeEnum)),
                    CreateEnumGroup("PlantSize", typeof(PlantSizeEnum)),
                    CreateEnumGroup("CareLevelType", typeof(CareLevelTypeEnum)),
                    CreateEnumGroup("FengShuiElement", typeof(FengShuiElementTypeEnum)),
                    CreateEnumGroup("SeasonType", typeof(SeasonTypeEnum))
                },
                FilterOptions = new List<StringOptionGroupResponseDto>(),
                SortEnums = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PlantSortBy", typeof(PlantSortByEnum)),
                    CreateEnumGroup("SortDirection", typeof(SortDirectionEnum))
                }
            };

            return Ok(new ApiResponse<SearchConfigResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant search config successfully",
                Payload = payload
            });
        }

        /// <summary>
        /// Metadata cho unified shop search
        /// GET /api/system/search-config/shop-unified
        /// </summary>
        [HttpGet("shop-unified")]
        [AllowAnonymous]
        public IActionResult GetShopUnifiedSearchConfig()
        {
            var payload = new SearchConfigResponseDto
            {
                FilterEnums = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PlacementType", typeof(PlacementTypeEnum)),
                    CreateEnumGroup("PlantSize", typeof(PlantSizeEnum)),
                    CreateEnumGroup("CareLevelType", typeof(CareLevelTypeEnum)),
                    CreateEnumGroup("FengShuiElement", typeof(FengShuiElementTypeEnum)),
                    CreateEnumGroup("SeasonType", typeof(SeasonTypeEnum)),
                    CreateEnumGroup("ComboType", typeof(ComboTypeEnum))
                },
                FilterOptions = new List<StringOptionGroupResponseDto>(),
                SortEnums = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("UnifiedSearchSortBy", typeof(UnifiedSearchSortByEnum)),
                    CreateEnumGroup("SortDirection", typeof(SortDirectionEnum))
                }
            };

            return Ok(new ApiResponse<SearchConfigResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get unified search config successfully",
                Payload = payload
            });
        }

        /// <summary>
        /// Metadata cho common plants shop search
        /// GET /api/system/search-config/common-plants
        /// </summary>
        [HttpGet("common-plants")]
        [AllowAnonymous]
        public IActionResult GetCommonPlantsSearchConfig()
        {
            var payload = new SearchConfigResponseDto
            {
                FilterEnums = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PlantSize", typeof(PlantSizeEnum))
                },
                FilterOptions = new List<StringOptionGroupResponseDto>(),
                SortEnums = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("CommonPlantSortBy", typeof(CommonPlantSortByEnum)),
                    CreateEnumGroup("SortDirection", typeof(SortDirectionEnum))
                }
            };

            return Ok(new ApiResponse<SearchConfigResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get common plants search config successfully",
                Payload = payload
            });
        }

        /// <summary>
        /// Metadata cho nursery materials shop search
        /// GET /api/system/search-config/nursery-materials
        /// </summary>
        [HttpGet("nursery-materials")]
        [AllowAnonymous]
        public IActionResult GetNurseryMaterialsSearchConfig()
        {
            var payload = new SearchConfigResponseDto
            {
                FilterEnums = new List<EnumGroupResponseDto>(),
                FilterOptions = new List<StringOptionGroupResponseDto>(),
                SortEnums = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("NurseryMaterialSortBy", typeof(NurseryMaterialSortByEnum)),
                    CreateEnumGroup("SortDirection", typeof(SortDirectionEnum))
                }
            };

            return Ok(new ApiResponse<SearchConfigResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get nursery materials search config successfully",
                Payload = payload
            });
        }

        /// <summary>
        /// Metadata cho plant combos shop search
        /// GET /api/system/search-config/plant-combos
        /// </summary>
        [HttpGet("plant-combos")]
        [AllowAnonymous]
        public IActionResult GetPlantCombosSearchConfig()
        {
            var payload = new SearchConfigResponseDto
            {
                FilterEnums = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("SeasonType", typeof(SeasonTypeEnum)),
                    CreateEnumGroup("ComboType", typeof(ComboTypeEnum))
                },
                FilterOptions = new List<StringOptionGroupResponseDto>
                {
                    CreateBooleanOptionGroup("PetSafe"),
                    CreateBooleanOptionGroup("ChildSafe")
                },
                SortEnums = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PlantComboSortBy", typeof(PlantComboSortByEnum)),
                    CreateEnumGroup("SortDirection", typeof(SortDirectionEnum))
                }
            };

            return Ok(new ApiResponse<SearchConfigResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant combos search config successfully",
                Payload = payload
            });
        }

        private static EnumGroupResponseDto CreateEnumGroup(string enumName, Type enumType)
        {
            return new EnumGroupResponseDto
            {
                GroupName = enumName,
                Values = Enum.GetValues(enumType)
                    .Cast<object>()
                    .Select(value => new EnumValueResponseDto
                    {
                        Value = Convert.ToInt32(value),
                        Name = value.ToString() ?? string.Empty
                    })
                    .OrderBy(item => item.Value)
                    .ToList()
            };
        }

        private static StringOptionGroupResponseDto CreateBooleanOptionGroup(string groupName)
        {
            return new StringOptionGroupResponseDto
            {
                GroupName = groupName,
                Values = new List<StringOptionResponseDto>
                {
                    new StringOptionResponseDto { Value = "true", Name = "true" },
                    new StringOptionResponseDto { Value = "false", Name = "false" }
                }
            };
        }

        public class SearchConfigResponseDto
        {
            public List<EnumGroupResponseDto> FilterEnums { get; set; } = new List<EnumGroupResponseDto>();
            public List<StringOptionGroupResponseDto> FilterOptions { get; set; } = new List<StringOptionGroupResponseDto>();
            public List<EnumGroupResponseDto> SortEnums { get; set; } = new List<EnumGroupResponseDto>();
        }

        public class EnumGroupResponseDto
        {
            public string GroupName { get; set; } = string.Empty;
            public List<EnumValueResponseDto> Values { get; set; } = new List<EnumValueResponseDto>();
        }

        public class EnumValueResponseDto
        {
            public int Value { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class StringOptionGroupResponseDto
        {
            public string GroupName { get; set; } = string.Empty;
            public List<StringOptionResponseDto> Values { get; set; } = new List<StringOptionResponseDto>();
        }

        public class StringOptionResponseDto
        {
            public string Value { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}