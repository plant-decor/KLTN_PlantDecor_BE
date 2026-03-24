using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class NurseryOrder
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int NurseryId { get; set; }

    public int? ShipperId { get; set; }

    public decimal? SubTotalAmount { get; set; }

    public decimal? DepositAmount { get; set; }

    public decimal? RemainingAmount { get; set; }

    public int? PaymentStrategy { get; set; }

    public int? Status { get; set; }

    public string? Note { get; set; }

    public string? ShipperNote { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Nursery Nursery { get; set; } = null!;

    public virtual User? Shipper { get; set; }

    public virtual ICollection<NurseryOrderDetail> NurseryOrderDetails { get; set; } = new List<NurseryOrderDetail>();
}