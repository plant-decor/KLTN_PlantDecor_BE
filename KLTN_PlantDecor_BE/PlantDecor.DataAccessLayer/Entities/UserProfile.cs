namespace PlantDecor.DataAccessLayer.Entities;

public partial class UserProfile
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? Address { get; set; }

    // Full birth date (replaces BirthYear). Backfill from BirthYear as DateOnly Jan 1 when migrating.
    public DateOnly? BirthDate { get; set; }

    // NOTE: Legacy `BirthYear` removed — use `BirthDate` instead.

    // Derived feng shui element computed from birth date/year
    public int? FengShuiElement { get; set; }

    // Stored customer tier (0=Bronze,1=Silver,2=Gold)
    //public int? CustomerTier { get; set; }

    public string? FullName { get; set; }

    public int? Gender { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool? ReceiveNotifications { get; set; }

    public string? NotificationPreferences { get; set; }

    public int? ProfileCompleteness { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? User { get; set; }
}
