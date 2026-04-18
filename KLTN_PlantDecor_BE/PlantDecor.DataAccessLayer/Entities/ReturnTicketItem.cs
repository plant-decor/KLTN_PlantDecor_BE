namespace PlantDecor.DataAccessLayer.Entities;

public partial class ReturnTicketItem
{
    public int Id { get; set; }

    public int ReturnTicketId { get; set; }

    public int NurseryOrderDetailId { get; set; }

    public int RequestedQuantity { get; set; }

    public int? ApprovedQuantity { get; set; }

    public string? Reason { get; set; }

    public int Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ReturnTicket ReturnTicket { get; set; } = null!;

    public virtual NurseryOrderDetail NurseryOrderDetail { get; set; } = null!;

    public virtual ICollection<ReturnTicketItemImage> ReturnTicketItemImages { get; set; } = new List<ReturnTicketItemImage>();
}
