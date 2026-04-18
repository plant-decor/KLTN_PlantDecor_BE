namespace PlantDecor.DataAccessLayer.Entities;

public partial class ReturnTicketItemImage
{
    public int Id { get; set; }

    public int ReturnTicketItemId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string? PublicId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ReturnTicketItem ReturnTicketItem { get; set; } = null!;
}
