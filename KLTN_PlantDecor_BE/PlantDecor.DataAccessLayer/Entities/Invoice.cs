using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Invoice
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public DateTime? IssuedDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? Type { get; set; }

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual Order? Order { get; set; }
}
