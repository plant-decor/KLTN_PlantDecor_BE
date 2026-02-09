using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class ServiceProgress
{
    public int Id { get; set; }

    public int? ServiceRegistrationId { get; set; }

    public int? CaretakerId { get; set; }

    public int? Action { get; set; }

    public string? Description { get; set; }

    public string? EvidenceImageUrl { get; set; }

    public DateTime? ActualStartTime { get; set; }

    public DateTime? ActualEndTime { get; set; }

    public virtual User? Caretaker { get; set; }

    public virtual ServiceRegistration? ServiceRegistration { get; set; }
}
