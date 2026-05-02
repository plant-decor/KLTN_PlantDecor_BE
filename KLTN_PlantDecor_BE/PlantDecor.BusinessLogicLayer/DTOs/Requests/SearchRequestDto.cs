using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class PaginationSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public bool? IsActive { get; set; }
    }

    public class UserSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public string? Keyword { get; set; }
        public RoleEnum? Role { get; set; }
        public UserStatusEnum? Status { get; set; }
        public bool? IsVerified { get; set; }
        public int? NurseryId { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }

    public class PlantSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public string? Keyword { get; set; }
        public bool? IsActive { get; set; }
        public int? PlacementType { get; set; }
        public int? CareLevelType { get; set; }
        public string? CareLevel { get; set; }
        public bool? Toxicity { get; set; }
        public bool? AirPurifying { get; set; }
        public bool? HasFlower { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public bool? IsUniqueInstance { get; set; }
        public decimal? MinBasePrice { get; set; }
        public decimal? MaxBasePrice { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? TagIds { get; set; }
        public List<int>? Sizes { get; set; }
        public int? FengShuiElement { get; set; }
        public int? NurseryId { get; set; }
        public PlantSortByEnum? SortBy { get; set; }
        public SortDirectionEnum? SortDirection { get; set; }
    }

    public class ShopPlantInstanceSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public int? NurseryId { get; set; }
        public int? PlantId { get; set; }
    }

    public class ShopPlantInstanceByNurserySearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public int? PlantId { get; set; }
    }
}