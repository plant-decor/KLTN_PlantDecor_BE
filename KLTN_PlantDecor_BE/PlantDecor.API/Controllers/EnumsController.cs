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
            ["OrderStatus"] = typeof(OrderStatusEnum),
            ["OrderType"] = typeof(OrderTypeEnum),
            ["InvoiceStatus"] = typeof(InvoiceStatusEnum),
            ["InvoiceType"] = typeof(InvoiceTypeEnum),
            ["PaymentStatus"] = typeof(PaymentStatusEnum),
            ["PaymentType"] = typeof(PaymentTypeEnum),
            ["PaymentStrategies"] = typeof(PaymentStrategiesEnum),
            ["TransactionStatus"] = typeof(TransactionStatusEnum),
            ["ComboType"] = typeof(ComboTypeEnum),
            ["Gender"] = typeof(GenderEnum),
            ["PlacementType"] = typeof(PlacementTypeEnum),
            ["PlantSize"] = typeof(PlantSizeEnum),
            ["CareLevelType"] = typeof(CareLevelTypeEnum),
            ["PlantSortBy"] = typeof(PlantSortByEnum),
            ["CommonPlantSortBy"] = typeof(CommonPlantSortByEnum),
            ["NurseryMaterialSortBy"] = typeof(NurseryMaterialSortByEnum),
            ["PlantComboSortBy"] = typeof(PlantComboSortByEnum),
            ["UnifiedSearchSortBy"] = typeof(UnifiedSearchSortByEnum),
            ["SortDirection"] = typeof(SortDirectionEnum),
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
                    CreateEnumGroup("PlacementType", typeof(PlacementTypeEnum)),
                    CreateEnumGroup("PlantSize", typeof(PlantSizeEnum)),
                    CreateEnumGroup("CareLevelType", typeof(CareLevelTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho sort của Plant search
        /// GET /api/system/enums/plant-sort
        /// </summary>
        [HttpGet("plant-sort")]
        [AllowAnonymous]
        public IActionResult GetPlantSortEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant sort enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PlantSortBy", typeof(PlantSortByEnum)),
                    CreateEnumGroup("SortDirection", typeof(SortDirectionEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho sort của CommonPlant search
        /// GET /api/system/enums/common-plant-sort
        /// </summary>
        [HttpGet("common-plant-sort")]
        [AllowAnonymous]
        public IActionResult GetCommonPlantSortEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get common plant sort enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("CommonPlantSortBy", typeof(CommonPlantSortByEnum)),
                    CreateEnumGroup("SortDirection", typeof(SortDirectionEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho sort của NurseryMaterial search
        /// GET /api/system/enums/nursery-material-sort
        /// </summary>
        [HttpGet("nursery-material-sort")]
        [AllowAnonymous]
        public IActionResult GetNurseryMaterialSortEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get nursery material sort enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("NurseryMaterialSortBy", typeof(NurseryMaterialSortByEnum)),
                    CreateEnumGroup("SortDirection", typeof(SortDirectionEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho sort của PlantCombo search
        /// GET /api/system/enums/plant-combo-sort
        /// </summary>
        [HttpGet("plant-combo-sort")]
        [AllowAnonymous]
        public IActionResult GetPlantComboSortEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant combo sort enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PlantComboSortBy", typeof(PlantComboSortByEnum)),
                    CreateEnumGroup("SortDirection", typeof(SortDirectionEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho sort của unified shop search
        /// GET /api/system/enums/unified-search-sort
        /// </summary>
        [HttpGet("unified-search-sort")]
        [AllowAnonymous]
        public IActionResult GetUnifiedSearchSortEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get unified search sort enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("UnifiedSearchSortBy", typeof(UnifiedSearchSortByEnum)),
                    CreateEnumGroup("SortDirection", typeof(SortDirectionEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho Plant Size
        /// GET /api/system/enums/plant-sizes
        /// </summary>
        [HttpGet("plant-sizes")]
        [AllowAnonymous]
        public IActionResult GetPlantSizeEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant size enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PlantSize", typeof(PlantSizeEnum))
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

        /// <summary>
        /// Enum cho Order
        /// GET /api/system/enums/orders
        /// </summary>
        [HttpGet("orders")]
        [AllowAnonymous]
        public IActionResult GetOrderEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get order enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("OrderStatus", typeof(OrderStatusEnum)),
                    CreateEnumGroup("OrderType", typeof(OrderTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho Invoice
        /// GET /api/system/enums/invoices
        /// </summary>
        [HttpGet("invoices")]
        [AllowAnonymous]
        public IActionResult GetInvoiceEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get invoice enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("InvoiceStatus", typeof(InvoiceStatusEnum)),
                    CreateEnumGroup("InvoiceType", typeof(InvoiceTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho Payment
        /// GET /api/system/enums/payments
        /// </summary>
        [HttpGet("payments")]
        [AllowAnonymous]
        public IActionResult GetPaymentEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get payment enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("PaymentStatus", typeof(PaymentStatusEnum)),
                    CreateEnumGroup("PaymentType", typeof(PaymentTypeEnum)),
                    CreateEnumGroup("PaymentStrategies", typeof(PaymentStrategiesEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho Transaction
        /// GET /api/system/enums/transactions
        /// </summary>
        [HttpGet("transactions")]
        [AllowAnonymous]
        public IActionResult GetTransactionEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get transaction enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("TransactionStatus", typeof(TransactionStatusEnum))
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