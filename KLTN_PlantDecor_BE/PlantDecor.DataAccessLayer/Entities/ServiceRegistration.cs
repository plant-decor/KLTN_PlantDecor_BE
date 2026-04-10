using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class ServiceRegistration
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? OrderId { get; set; }

    public int? NurseryCareServiceId { get; set; }

    public int? MainCaretakerId { get; set; }

    public int? CurrentCaretakerId { get; set; }

    public int? PrefferedShiftId { get; set; }

    public int? Status { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateOnly? ServiceDate { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Address { get; set; }

    public decimal? Longitude { get; set; }

    public decimal? Latitude { get; set; }

    public string? ScheduleDaysOfWeek { get; set; }

    public string? Phone { get; set; }

    public int? TotalSessions { get; set; }

    public string? CancelReason { get; set; }

    public virtual User? CurrentCaretaker { get; set; }

    public virtual User? MainCaretaker { get; set; }

    public virtual Order? Order { get; set; }

    public virtual NurseryCareService? NurseryCareService { get; set; }

    public virtual ICollection<ServiceProgress> ServiceProgresses { get; set; } = new List<ServiceProgress>();

    public virtual User? User { get; set; }

    public virtual ServiceRating? ServiceRating { get; set; }

    public virtual Shift? PrefferedShift { get; set; }
}
