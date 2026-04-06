namespace PlantDecor.DataAccessLayer.Entities;

public partial class Invoice
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public DateTime? IssuedDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public int? Type { get; set; }

    public int? Status { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerEmail { get; set; }

    public string? CustomerAddress { get; set; }

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Order? Order { get; set; }
}
