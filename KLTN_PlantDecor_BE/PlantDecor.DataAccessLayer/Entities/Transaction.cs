using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Transaction
{
    public int Id { get; set; }

    public int? PaymentId { get; set; }

    public decimal? Amount { get; set; }

    public int? Status { get; set; }

    public string? TransactionId { get; set; }

    public string? ResponseCode { get; set; }

    public string? OrderInfo { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public virtual Payment? Payment { get; set; }
}
