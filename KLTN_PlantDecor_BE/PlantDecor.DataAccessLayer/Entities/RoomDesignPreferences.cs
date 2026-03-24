namespace PlantDecor.DataAccessLayer.Entities;

public partial class RoomDesignPreferences
{
    public int Id { get; set; }

    public int RoomImageId { get; set; }

    public int? RoomType { get; set; }

    public int? RoomStyle { get; set; }

    public int? RoomArea { get; set; }

    public decimal? MinBudget { get; set; }

    public decimal? MaxBudget { get; set; }

    public int? CareLevel { get; set; }

    public bool? IsOftenAway { get; set; }

    public int? NaturalLightLevel { get; set; }

    public bool? HasAllergy { get; set; }

    public string? AllergyNote { get; set; }

    public virtual RoomImage RoomImage { get; set; } = null!;
}
