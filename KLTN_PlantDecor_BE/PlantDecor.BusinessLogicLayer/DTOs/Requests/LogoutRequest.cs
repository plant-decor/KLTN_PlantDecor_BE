namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class LogoutRequest
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string DeviceId { get; set; }
    }
}
