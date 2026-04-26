namespace PlantDecor.DataAccessLayer.Entities;

public partial class User
{
    public int Id { get; set; }

    public int? RoleId { get; set; }

    public int? NurseryId { get; set; } // nursery đang làm việc

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? Username { get; set; }

    public string? AvatarUrl { get; set; }

    public int? Status { get; set; }
    public bool IsVerified { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? SecurityStamp { get; set; }

    public virtual ICollection<AIChatSession> AIChatSessions { get; set; } = new List<AIChatSession>();

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();

    public virtual ICollection<LayoutDesign> LayoutDesigns { get; set; } = new List<LayoutDesign>();

    public virtual Nursery? WorkingNursery { get; set; }
    public virtual Nursery? ManagedNursery { get; set; }

    public virtual ICollection<Order> CustomerOrders { get; set; } = new List<Order>();

    public virtual ICollection<NurseryOrder> ShipperNurseryOrders { get; set; } = new List<NurseryOrder>();

    public virtual ICollection<PlantRating> PlantRatings { get; set; } = new List<PlantRating>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();

    public virtual ICollection<ServiceProgress> ServiceProgresses { get; set; } = new List<ServiceProgress>();

    public virtual ICollection<ServiceRegistration> ServiceRegistrationCurrentCaretakers { get; set; } = new List<ServiceRegistration>();

    public virtual ICollection<ServiceRegistration> ServiceRegistrationMainCaretakers { get; set; } = new List<ServiceRegistration>();

    public virtual ICollection<ServiceRegistration> ServiceRegistrationUsers { get; set; } = new List<ServiceRegistration>();

    public virtual ICollection<DesignRegistration> DesignRegistrationsUser { get; set; } = new List<DesignRegistration>();

    public virtual ICollection<DesignTask> AssignedDesignTasks { get; set; } = new List<DesignTask>();

    public virtual ICollection<DesignRegistration> DesignRegistrationsAssignedCaretaker { get; set; } = new List<DesignRegistration>();

    public virtual ICollection<UserBehaviorLog> UserBehaviorLogs { get; set; } = new List<UserBehaviorLog>();

    public virtual ICollection<UserPlant> UserPlants { get; set; } = new List<UserPlant>();

    public virtual ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();

    public virtual UserProfile? UserProfile { get; set; }

    public virtual ServiceRating? ServiceRating { get; set; }

    public virtual CustomerSurvey? CustomerSurvey { get; set; }

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public virtual ICollection<StaffSpecialization> StaffSpecializations { get; set; } = new List<StaffSpecialization>();
}
