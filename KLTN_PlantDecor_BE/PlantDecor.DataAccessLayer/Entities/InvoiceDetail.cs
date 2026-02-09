using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class InvoiceDetail
{
    public int Id { get; set; }

    public int? InvoiceId { get; set; }

    public string? ItemName { get; set; }

    public decimal? UnitPrice { get; set; }

    public int? Quantity { get; set; }

    public decimal? Amount { get; set; }

    public virtual Invoice? Invoice { get; set; }
}
