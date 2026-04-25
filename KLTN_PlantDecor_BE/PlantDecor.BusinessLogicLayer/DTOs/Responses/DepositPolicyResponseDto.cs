namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class DepositPolicyResponseDto
    {
        public int Id { get; set; }
        public decimal MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int DepositPercentage { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
