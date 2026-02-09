using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class UserProfile
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Address { get; set; }

    public int? BirthYear { get; set; }

    public string? FullName { get; set; }

    public int? Gender { get; set; }

    public bool? ReceiveNotifications { get; set; }

    public string? NotificationPreferences { get; set; }

    public int? ProfileCompleteness { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? User { get; set; }
}
