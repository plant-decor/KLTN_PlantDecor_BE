namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class OtpResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public DateTime? ExpiresAt { get; set; }
    }
}
