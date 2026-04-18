namespace PlantDecor.DataAccessLayer.Entities;

public partial class ReturnTicket
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public string? Reason { get; set; }

    public int Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User Customer { get; set; } = null!;

    public virtual ICollection<ReturnTicketItem> ReturnTicketItems { get; set; } = new List<ReturnTicketItem>();

    public virtual ICollection<ReturnTicketAssignment> ReturnTicketAssignments { get; set; } = new List<ReturnTicketAssignment>();
}
