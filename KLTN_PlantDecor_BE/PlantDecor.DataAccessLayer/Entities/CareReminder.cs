using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class CareReminder
{
    public int Id { get; set; }

    public int? UserPlantId { get; set; }

    public int? CareType { get; set; }

    public string? Content { get; set; }

    public DateOnly? ReminderDate { get; set; }

    public DateOnly? ScheduledDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual UserPlant? UserPlant { get; set; }
}
