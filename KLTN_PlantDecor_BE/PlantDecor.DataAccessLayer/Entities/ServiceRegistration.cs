using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class ServiceRegistration
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? OrderId { get; set; }

    public int? ServiceId { get; set; }

    public int? MainCaretakerId { get; set; }

    public int? CurrentCaretakerId { get; set; }

    public int? Status { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ServiceDate { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? CancelReason { get; set; }

    public int? EstimatedDuration { get; set; }

    public virtual User? CurrentCaretaker { get; set; }

    public virtual User? MainCaretaker { get; set; }

    public virtual Order? Order { get; set; }

    public virtual CareServicePackage? Service { get; set; }

    public virtual ICollection<ServiceProgress> ServiceProgresses { get; set; } = new List<ServiceProgress>();

    public virtual ICollection<ServiceRating> ServiceRatings { get; set; } = new List<ServiceRating>();

    public virtual User? User { get; set; }
}
