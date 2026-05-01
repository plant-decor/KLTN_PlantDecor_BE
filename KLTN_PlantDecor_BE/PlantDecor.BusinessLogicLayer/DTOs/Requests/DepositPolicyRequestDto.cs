namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class DepositPolicyRequestDto
    {
        public decimal MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int DepositPercentage { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateDepositPolicyRequestDto
    {
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? DepositPercentage { get; set; }
        public bool? IsActive { get; set; }
    }
}
