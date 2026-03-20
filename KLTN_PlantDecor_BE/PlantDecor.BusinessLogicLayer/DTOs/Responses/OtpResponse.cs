using System.Text.Json.Serialization;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class OtpResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? ExpiresAt { get; set; }
    }
}
