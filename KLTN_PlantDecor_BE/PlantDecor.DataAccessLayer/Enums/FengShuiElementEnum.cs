using System.Text.Json.Serialization;

namespace PlantDecor.DataAccessLayer.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FengShuiElementEnum
    {
        Kim,
        Moc,
        Thuy,
        Hoa,
        Tho
    }
}
