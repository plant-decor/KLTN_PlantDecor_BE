namespace PlantDecor.DataAccessLayer.Entities;

public partial class ReturnTicketAssignment
{
    public int Id { get; set; }

    public int ReturnTicketId { get; set; }

    public int NurseryId { get; set; }

    public int? ManagerId { get; set; }

    public int Status { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ReturnTicket ReturnTicket { get; set; } = null!;

    public virtual Nursery Nursery { get; set; } = null!;

    public virtual User? Manager { get; set; }
}
