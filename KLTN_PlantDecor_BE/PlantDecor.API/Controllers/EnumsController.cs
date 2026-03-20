using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API trả về các giá trị enum cho frontend
    /// </summary>
    [Route("api/system/enums")]
    [ApiController]
    public class EnumsController : ControllerBase
    {
        private static readonly Dictionary<string, Type> EnumMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CategoryType"] = typeof(CategoryTypeEnum),
            ["ComboType"] = typeof(ComboTypeEnum),
            ["Gender"] = typeof(GenderEnum),
            ["PlacementType"] = typeof(PlacementTypeEnum),
            ["PlantInstanceStatus"] = typeof(PlantInstanceStatusEnum),
            ["Role"] = typeof(RoleEnum),
            ["TagType"] = typeof(TagTypeEnum),
            ["UserActionType"] = typeof(UserActionTypeEnum),
            ["UserStatus"] = typeof(UserStatusEnum)
        };

        /// <summary>
        /// Lấy tất cả enum values của hệ thống
        /// GET /api/system/enums
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAllEnums()
        {
            var payload = EnumMap
                .Select(item => CreateEnumGroup(item.Key, item.Value))
                .ToList();

            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get all enum values successfully",
                Payload = payload
            });
        }

        /// <summary>
        /// Lấy enum values theo tên enum
        /// GET /api/system/enums/{enumName}
        /// </summary>
        [HttpGet("{enumName}")]
        [AllowAnonymous]
        public IActionResult GetEnumByName(string enumName)
        {
            if (!EnumMap.TryGetValue(enumName, out var enumType))
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Enum '{enumName}' không tồn tại"
                });
            }

            return Ok(new ApiResponse<EnumGroupResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get enum values successfully",
                Payload = CreateEnumGroup(enumName, enumType)
            });
        }

        /// <summary>
        /// Enum cho Category
        /// GET /api/system/enums/categories
        /// </summary>
        [HttpGet("categories")]
        [AllowAnonymous]
        public IActionResult GetCategoryEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get category enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("CategoryType", typeof(CategoryTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho Plant
        /// GET /api/system/enums/plants
        /// </summary>
        [HttpGet("plants")]
        [AllowAnonymous]
        public IActionResult GetPlantEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PlacementType", typeof(PlacementTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho PlantInstance
        /// GET /api/system/enums/plant-instances
        /// </summary>
        [HttpGet("plant-instances")]
        [AllowAnonymous]
        public IActionResult GetPlantInstanceEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant instance enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PlantInstanceStatus", typeof(PlantInstanceStatusEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho Combo
        /// GET /api/system/enums/combos
        /// </summary>
        [HttpGet("combos")]
        [AllowAnonymous]
        public IActionResult GetComboEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get combo enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("ComboType", typeof(ComboTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho Tag
        /// GET /api/system/enums/tags
        /// </summary>
        [HttpGet("tags")]
        [AllowAnonymous]
        public IActionResult GetTagEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get tag enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("TagType", typeof(TagTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho User
        /// GET /api/system/enums/users
        /// </summary>
        [HttpGet("users")]
        [AllowAnonymous]
        public IActionResult GetUserEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get user enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("Gender", typeof(GenderEnum)),
                    CreateEnumGroup("UserStatus", typeof(UserStatusEnum)),
                    CreateEnumGroup("UserActionType", typeof(UserActionTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho Role
        /// GET /api/system/enums/roles
        /// </summary>
        [HttpGet("roles")]
        [AllowAnonymous]
        public IActionResult GetRoleEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get role enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("Role", typeof(RoleEnum))
                }
            });
        }

        private static EnumGroupResponseDto CreateEnumGroup(string enumName, Type enumType)
        {
            return new EnumGroupResponseDto
            {
                EnumName = enumName,
                Values = GetEnumValues(enumType)
            };
        }

        private static List<EnumValueResponseDto> GetEnumValues(Type enumType)
        {
            return Enum.GetValues(enumType)
                .Cast<object>()
                .Select(value => new EnumValueResponseDto
                {
                    Value = Convert.ToInt32(value),
                    Name = value.ToString() ?? string.Empty
                })
                .OrderBy(item => item.Value)
                .ToList();
        }

        public class EnumGroupResponseDto
        {
            public string EnumName { get; set; } = string.Empty;
            public List<EnumValueResponseDto> Values { get; set; } = new List<EnumValueResponseDto>();
        }

        public class EnumValueResponseDto
        {
            public int Value { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}