namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class NurseryOrderShipperResponseDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    public int TotalOrdersInDay { get; set; }
    }
}
