namespace PlantDecor.DataAccessLayer.Entities;

public partial class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? CustomerName { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? DepositAmount { get; set; }

    public decimal? RemainingAmount { get; set; }

    public int? Status { get; set; }

    public string? Note { get; set; }

    public int? PaymentStrategy { get; set; }

    public string? ReturnReason { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? OrderType { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<NurseryOrder> NurseryOrders { get; set; } = new List<NurseryOrder>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ServiceRegistration? ServiceRegistration { get; set; }
    public virtual DesignRegistration? DesignRegistration { get; set; }

    public virtual User Customer { get; set; } = null!;
}
