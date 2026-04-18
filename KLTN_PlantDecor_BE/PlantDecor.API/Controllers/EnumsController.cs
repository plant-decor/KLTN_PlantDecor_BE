using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
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
        private readonly IShiftService _shiftService;

        public EnumsController(IShiftService shiftService)
        {
            _shiftService = shiftService;
        }

        private static readonly Dictionary<string, Type> EnumMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CategoryType"] = typeof(CategoryTypeEnum),
            ["OrderStatus"] = typeof(OrderStatusEnum),
            ["OrderType"] = typeof(OrderTypeEnum),
            ["BuyNowItemType"] = typeof(BuyNowItemTypeEnum),
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
            ["GrowthRate"] = typeof(GrowthRateEnum),
            ["CareLevelType"] = typeof(CareLevelTypeEnum),
            ["PlantSortBy"] = typeof(PlantSortByEnum),
            ["CommonPlantSortBy"] = typeof(CommonPlantSortByEnum),
            ["NurseryMaterialSortBy"] = typeof(NurseryMaterialSortByEnum),
            ["PlantComboSortBy"] = typeof(PlantComboSortByEnum),
            ["UnifiedSearchSortBy"] = typeof(UnifiedSearchSortByEnum),
            ["SortDirection"] = typeof(SortDirectionEnum),
            ["PlantInstanceStatus"] = typeof(PlantInstanceStatusEnum),
            ["RoomType"] = typeof(RoomTypeEnum),
            ["RoomStyle"] = typeof(RoomStyleEnum),
            ["LightRequirement"] = typeof(LightRequirementEnum),
            ["LayoutDesignStatus"] = typeof(LayoutDesignStatusEnum),
            ["RoomUploadModerationStatus"] = typeof(RoomUploadModerationStatusEnum),
            ["AiLayoutResponseModerationStatus"] = typeof(AilayoutResponseModerationStatus),
            ["Role"] = typeof(RoleEnum),
            ["TagType"] = typeof(TagTypeEnum),
            ["WishlistItemType"] = typeof(WishlistItemType),
            ["UserActionType"] = typeof(UserActionTypeEnum),
            ["UserStatus"] = typeof(UserStatusEnum),
            ["CareServiceType"] = typeof(CareServiceTypeEnum),
            ["ServiceRegistrationStatus"] = typeof(ServiceRegistrationStatusEnum),
            ["ServiceProgressStatus"] = typeof(ServiceProgressStatusEnum),
            ["DesignRegistrationStatus"] = typeof(DesignRegistrationStatus),
            ["DesignTaskStatus"] = typeof(DesignTaskStatusEnum),
            ["TaskType"] = typeof(TaskTypeEnum),
            ["DayOfWeek"] = typeof(DayOfWeek)
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
        /// Enum cho Care Service
        /// GET /api/system/enums/care-services
        /// </summary>
        [HttpGet("care-services")]
        [AllowAnonymous]
        public IActionResult GetCareServiceEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get care service enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("CareServiceType", typeof(CareServiceTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho ServiceRegistration
        /// GET /api/system/enums/service-registrations
        /// </summary>
        [HttpGet("service-registrations")]
        [AllowAnonymous]
        public IActionResult GetServiceRegistrationEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get service registration enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("ServiceRegistrationStatus", typeof(ServiceRegistrationStatusEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho ServiceProgress
        /// GET /api/system/enums/service-progress
        /// </summary>
        [HttpGet("service-progress")]
        [AllowAnonymous]
        public IActionResult GetServiceProgressEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get service progress enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("ServiceProgressStatus", typeof(ServiceProgressStatusEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho DesignRegistration/DesignTask
        /// GET /api/system/enums/design-flow
        /// </summary>
        [HttpGet("design-flow")]
        [AllowAnonymous]
        public IActionResult GetDesignFlowEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get design flow enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("DesignRegistrationStatus", typeof(DesignRegistrationStatus)),
                    CreateEnumGroup("DesignTaskStatus", typeof(DesignTaskStatusEnum)),
                    CreateEnumGroup("TaskType", typeof(TaskTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum + dữ liệu phục vụ flow ServiceRegistration/ServiceProgress trong 1 lần fetch
        /// GET /api/system/enums/service-flow
        /// </summary>
        [HttpGet("service-flow")]
        [AllowAnonymous]
        public async Task<IActionResult> GetServiceFlowEnums()
        {
            var shifts = await _shiftService.GetAllAsync();

            return Ok(new ApiResponse<ServiceFlowEnumsResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get service flow enums successfully",
                Payload = new ServiceFlowEnumsResponseDto
                {
                    Enums = new List<EnumGroupResponseDto>
                    {
                        CreateEnumGroup("ServiceRegistrationStatus", typeof(ServiceRegistrationStatusEnum)),
                        CreateEnumGroup("ServiceProgressStatus", typeof(ServiceProgressStatusEnum)),
                        CreateServiceDayOfWeekGroup(),
                        CreateEnumGroup("CareServiceType", typeof(CareServiceTypeEnum))
                    },
                    Shifts = shifts
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
                    CreateEnumGroup("GrowthRate", typeof(GrowthRateEnum)),
                    CreateEnumGroup("CareLevelType", typeof(CareLevelTypeEnum))
                }
            });
        }

        /// <summary>
        /// Enum cho Room Design
        /// GET /api/system/enums/room-design
        /// </summary>
        [HttpGet("room-design")]
        [AllowAnonymous]
        public IActionResult GetRoomDesignEnums()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get room design enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("RoomType", typeof(RoomTypeEnum)),
                    CreateEnumGroup("RoomStyle", typeof(RoomStyleEnum)),
                    CreateEnumGroup("LightRequirement", typeof(LightRequirementEnum)),
                    CreateEnumGroup("LayoutDesignStatus", typeof(LayoutDesignStatusEnum)),
                    CreateEnumGroup("RoomUploadModerationStatus", typeof(RoomUploadModerationStatusEnum)),
                    CreateEnumGroup("AiLayoutResponseModerationStatus", typeof(AilayoutResponseModerationStatus))
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
        /// Enum cho Wishlist item type
        /// GET /api/system/enums/wishlist-types
        /// </summary>
        [HttpGet("wishlist-types")]
        [AllowAnonymous]
        public IActionResult GetWishlistTypeEnum()
        {
            return Ok(new ApiResponse<List<EnumGroupResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get wishlist enums successfully",
                Payload = new List<EnumGroupResponseDto>
                {
                    CreateEnumGroup("WishlistItemType", typeof(WishlistItemType))
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
                    CreateEnumGroup("OrderType", typeof(OrderTypeEnum)),
                    CreateEnumGroup("BuyNowItemType", typeof(BuyNowItemTypeEnum))
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

        private static EnumGroupResponseDto CreateServiceDayOfWeekGroup()
        {
            return new EnumGroupResponseDto
            {
                EnumName = "DayOfWeek",
                Values = GetEnumValues(typeof(DayOfWeek))
                    .Where(item => item.Value >= 1 && item.Value <= 6)
                    .ToList()
            };
        }

        public class ServiceFlowEnumsResponseDto
        {
            public List<EnumGroupResponseDto> Enums { get; set; } = new List<EnumGroupResponseDto>();
            public List<ShiftResponseDto> Shifts { get; set; } = new List<ShiftResponseDto>();
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