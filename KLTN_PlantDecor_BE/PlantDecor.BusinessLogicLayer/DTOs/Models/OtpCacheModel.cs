namespace PlantDecor.BusinessLogicLayer.DTOs.Models
{
    public class OtpCacheModel
    {
        public string OtpCode { get; set; } = null!;
        public string Purpose { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
    }
}
