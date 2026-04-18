using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class ServiceProgress
{
    public int Id { get; set; }

    public int? ServiceRegistrationId { get; set; }

    public int? CaretakerId { get; set; }

    public int ShiftId { get; set; }

    public string? Description { get; set; }

    public string? EvidenceImageUrl { get; set; }

    public DateOnly? TaskDate { get; set; }

    public string? CustomerNote { get; set; }

    public bool HasIncidents { get; set; }

    public string? IncidentImageUrl { get; set; }

    public string? IncidentReason { get; set; }

    public int? Status { get; set; }

    public DateTime? ActualStartTime { get; set; }

    public DateTime? ActualEndTime { get; set; }

    public virtual User? Caretaker { get; set; }

    public virtual ServiceRegistration? ServiceRegistration { get; set; }

    public virtual Shift Shift { get; set; } = null!;
}
