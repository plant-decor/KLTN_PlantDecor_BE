using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class ConsultantOrderSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public int? Status { get; set; }
        public int? OrderType { get; set; }
        public int? PaymentStrategy { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public decimal? MinTotalAmount { get; set; }
        public decimal? MaxTotalAmount { get; set; }
        public string? CustomerEmail { get; set; }
        public OrderSortByEnum? SortBy { get; set; }
        public SortDirectionEnum? SortDirection { get; set; }
    }
}
