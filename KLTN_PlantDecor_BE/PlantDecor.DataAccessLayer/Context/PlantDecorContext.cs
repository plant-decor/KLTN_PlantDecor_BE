using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Context;

public partial class PlantDecorContext : DbContext
{
    public PlantDecorContext()
    {
    }

    public PlantDecorContext(DbContextOptions<PlantDecorContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AilayoutResponseModeration> AilayoutResponseModerations { get; set; }

    public virtual DbSet<CareReminder> CareReminders { get; set; }

    public virtual DbSet<CareServicePackage> CareServicePackages { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChatParticipant> ChatParticipants { get; set; }

    public virtual DbSet<ChatSession> ChatSessions { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<InventoryImage> InventoryImages { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }

    public virtual DbSet<LayoutDesign> LayoutDesigns { get; set; }

    public virtual DbSet<Nursery> Nurseries { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Plant> Plants { get; set; }

    public virtual DbSet<PlantCombo> PlantCombos { get; set; }

    public virtual DbSet<PlantComboImage> PlantComboImages { get; set; }

    public virtual DbSet<PlantComboItem> PlantComboItems { get; set; }

    public virtual DbSet<PlantGuide> PlantGuides { get; set; }

    public virtual DbSet<PlantImage> PlantImages { get; set; }

    public virtual DbSet<PlantInstance> PlantInstances { get; set; }

    public virtual DbSet<PlantRating> PlantRatings { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoomImage> RoomImages { get; set; }

    public virtual DbSet<RoomUploadModeration> RoomUploadModerations { get; set; }

    public virtual DbSet<ServiceProgress> ServiceProgresses { get; set; }

    public virtual DbSet<ServiceRating> ServiceRatings { get; set; }

    public virtual DbSet<ServiceRegistration> ServiceRegistrations { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserBehaviorLog> UserBehaviorLogs { get; set; }

    public virtual DbSet<UserPlant> UserPlants { get; set; }

    public virtual DbSet<UserPreference> UserPreferences { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AilayoutResponseModeration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AILayoutResponseModeration_pkey");

            entity.ToTable("AILayoutResponseModeration");

            entity.Property(e => e.Reason).HasMaxLength(255);

            entity.HasOne(d => d.LayoutDesign).WithMany(p => p.AilayoutResponseModerations)
                .HasForeignKey(d => d.LayoutDesignId)
                .HasConstraintName("AILayoutResponseModeration_LayoutDesignId_fkey");
        });

        modelBuilder.Entity<CareReminder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CareReminder_pkey");

            entity.ToTable("CareReminder");

            entity.Property(e => e.Content).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.UserPlant).WithMany(p => p.CareReminders)
                .HasForeignKey(d => d.UserPlantId)
                .HasConstraintName("CareReminder_UserPlantId_fkey");
        });

        modelBuilder.Entity<CareServicePackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CareServicePackage_pkey");

            entity.ToTable("CareServicePackage");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Frequency).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(d => d.ParentService).WithMany(p => p.InverseParentService)
                .HasForeignKey(d => d.ParentServiceId)
                .HasConstraintName("CareServicePackage_ParentServiceId_fkey");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Cart_pkey");

            entity.ToTable("Cart");

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("Cart_UserId_fkey");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CartItem_pkey");

            entity.ToTable("CartItem");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("CartItem_CartId_fkey");

            entity.HasOne(d => d.Inventory).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.InventoryId)
                .HasConstraintName("CartItem_InventoryId_fkey");

            entity.HasOne(d => d.PlantCombo).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.PlantComboId)
                .HasConstraintName("CartItem_PlantComboId_fkey");

            entity.HasOne(d => d.Plant).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("CartItem_PlantId_fkey");

            entity.HasOne(d => d.PlantInstance).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.PlantInstanceId)
                .HasConstraintName("CartItem_PlantInstanceId_fkey");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Category_pkey");

            entity.ToTable("Category");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .HasConstraintName("Category_ParentCategoryId_fkey");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ChatMessage_pkey");

            entity.ToTable("ChatMessage");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.ChatSession).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ChatSessionId)
                .HasConstraintName("ChatMessage_ChatSessionId_fkey");
        });

        modelBuilder.Entity<ChatParticipant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ChatParticipant_pkey");

            entity.ToTable("ChatParticipant");

            entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.ChatSession).WithMany(p => p.ChatParticipants)
                .HasForeignKey(d => d.ChatSessionId)
                .HasConstraintName("ChatParticipant_ChatSessionId_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ChatParticipants)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("ChatParticipant_UserId_fkey");
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ChatSession_pkey");

            entity.ToTable("ChatSession");

            entity.Property(e => e.StartedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Inventory_pkey");

            entity.ToTable("Inventory");

            entity.Property(e => e.BasePrice).HasPrecision(18, 2);
            entity.Property(e => e.Brand).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.InventoryCode).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Specifications).HasColumnType("jsonb");
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasMany(d => d.Categories).WithMany(p => p.Inventories)
                .UsingEntity<Dictionary<string, object>>(
                    "InventoryCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("InventoryCategory_CategoryId_fkey"),
                    l => l.HasOne<Inventory>().WithMany()
                        .HasForeignKey("InventoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("InventoryCategory_InventoryId_fkey"),
                    j =>
                    {
                        j.HasKey("InventoryId", "CategoryId").HasName("InventoryCategory_pkey");
                        j.ToTable("InventoryCategory");
                    });

            entity.HasMany(d => d.Tags).WithMany(p => p.Inventories)
                .UsingEntity<Dictionary<string, object>>(
                    "InventoryTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("InventoryTag_TagId_fkey"),
                    l => l.HasOne<Inventory>().WithMany()
                        .HasForeignKey("InventoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("InventoryTag_InventoryId_fkey"),
                    j =>
                    {
                        j.HasKey("InventoryId", "TagId").HasName("InventoryTag_pkey");
                        j.ToTable("InventoryTag");
                    });
        });

        modelBuilder.Entity<InventoryImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("InventoryImage_pkey");

            entity.ToTable("InventoryImage");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ImageUrl).HasMaxLength(512);
            entity.Property(e => e.IsPrimary).HasDefaultValue(false);

            entity.HasOne(d => d.Inventory).WithMany(p => p.InventoryImages)
                .HasForeignKey(d => d.InventoryId)
                .HasConstraintName("InventoryImage_InventoryId_fkey");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Invoice_pkey");

            entity.ToTable("Invoice");

            entity.Property(e => e.IssuedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.Order).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("Invoice_OrderId_fkey");
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("InvoiceDetail_pkey");

            entity.ToTable("InvoiceDetail");

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.ItemName).HasMaxLength(255);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("InvoiceDetail_InvoiceId_fkey");
        });

        modelBuilder.Entity<LayoutDesign>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("LayoutDesign_pkey");

            entity.ToTable("LayoutDesign");

            entity.Property(e => e.AllowedToAll).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.PreviewImageUrl).HasMaxLength(512);

            entity.HasOne(d => d.User).WithMany(p => p.LayoutDesigns)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("LayoutDesign_UserId_fkey");
        });

        modelBuilder.Entity<Nursery>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Nursery_pkey");

            entity.ToTable("Nursery");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);

            entity.HasOne(d => d.Manager).WithMany(p => p.Nurseries)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("Nursery_ManagerId_fkey");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Order_pkey");

            entity.ToTable("Order");

            entity.HasIndex(e => e.Status, "IX_Order_Status");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.DepositAmount).HasPrecision(18, 2);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.RemainingAmount).HasPrecision(18, 2);
            entity.Property(e => e.ReturnReason).HasMaxLength(255);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("Order_UserId_fkey");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("OrderItem_pkey");

            entity.ToTable("OrderItem");

            entity.HasIndex(e => e.OrderId, "IX_OrderItem_OrderId");

            entity.HasIndex(e => e.PlantComboId, "IX_OrderItem_PlantComboId");

            entity.Property(e => e.ItemName).HasMaxLength(255);
            entity.Property(e => e.Price).HasPrecision(18, 2);

            entity.HasOne(d => d.Inventory).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.InventoryId)
                .HasConstraintName("OrderItem_InventoryId_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("OrderItem_OrderId_fkey");

            entity.HasOne(d => d.PlantCombo).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.PlantComboId)
                .HasConstraintName("OrderItem_PlantComboId_fkey");

            entity.HasOne(d => d.Plant).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("OrderItem_PlantId_fkey");

            entity.HasOne(d => d.PlantInstance).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.PlantInstanceId)
                .HasConstraintName("OrderItem_PlantInstanceId_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Payment_pkey");

            entity.ToTable("Payment");

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("Payment_OrderId_fkey");
        });

        modelBuilder.Entity<Plant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Plant_pkey");

            entity.ToTable("Plant");

            entity.HasIndex(e => e.Name, "IX_Plant_Name");

            entity.HasIndex(e => e.PlantCode, "Plant_PlantCode_key").IsUnique();

            entity.Property(e => e.BasePrice).HasPrecision(18, 2);
            entity.Property(e => e.CareLevel).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.FengShuiElement).HasMaxLength(50);
            entity.Property(e => e.GrowthRate).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Origin).HasMaxLength(100);
            entity.Property(e => e.Placement).HasMaxLength(50);
            entity.Property(e => e.PlantCode).HasMaxLength(50);
            entity.Property(e => e.PlantType).HasMaxLength(50);
            entity.Property(e => e.PotSize).HasMaxLength(50);
            entity.Property(e => e.Size).HasMaxLength(50);
            entity.Property(e => e.SpecificName).HasMaxLength(255);
            entity.Property(e => e.Texture).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasMany(d => d.Categories).WithMany(p => p.Plants)
                .UsingEntity<Dictionary<string, object>>(
                    "PlantCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("PlantCategory_CategoryId_fkey"),
                    l => l.HasOne<Plant>().WithMany()
                        .HasForeignKey("PlantId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("PlantCategory_PlantId_fkey"),
                    j =>
                    {
                        j.HasKey("PlantId", "CategoryId").HasName("PlantCategory_pkey");
                        j.ToTable("PlantCategory");
                    });

            entity.HasMany(d => d.Tags).WithMany(p => p.Plants)
                .UsingEntity<Dictionary<string, object>>(
                    "PlantTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("PlantTag_TagId_fkey"),
                    l => l.HasOne<Plant>().WithMany()
                        .HasForeignKey("PlantId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("PlantTag_PlantId_fkey"),
                    j =>
                    {
                        j.HasKey("PlantId", "TagId").HasName("PlantTag_pkey");
                        j.ToTable("PlantTag");
                    });
        });

        modelBuilder.Entity<PlantCombo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PlantCombo_pkey");

            entity.ToTable("PlantCombo");

            entity.Property(e => e.ComboCode).HasMaxLength(50);
            entity.Property(e => e.ComboName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.DiscountPercent).HasPrecision(5, 2);
            entity.Property(e => e.FengShuiElement).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.OriginalPrice).HasPrecision(18, 2);
            entity.Property(e => e.PurchaseCount).HasDefaultValue(0);
            entity.Property(e => e.SalePrice).HasPrecision(18, 2);
            entity.Property(e => e.Season).HasMaxLength(50);
            entity.Property(e => e.SuitableRooms).HasColumnType("jsonb");
            entity.Property(e => e.SuitableSpace).HasMaxLength(100);
            entity.Property(e => e.ThemeName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ViewCount).HasDefaultValue(0);

            entity.HasMany(d => d.TagsNavigation).WithMany(p => p.PlantCombos)
                .UsingEntity<Dictionary<string, object>>(
                    "ComboTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("ComboTag_TagId_fkey"),
                    l => l.HasOne<PlantCombo>().WithMany()
                        .HasForeignKey("PlantComboId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("ComboTag_PlantComboId_fkey"),
                    j =>
                    {
                        j.HasKey("PlantComboId", "TagId").HasName("ComboTag_pkey");
                        j.ToTable("ComboTag");
                    });
        });

        modelBuilder.Entity<PlantComboImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PlantComboImage_pkey");

            entity.ToTable("PlantComboImage");

            entity.HasIndex(e => e.PlantComboId, "IX_PlantComboImage_ComboId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ImageUrl).HasMaxLength(512);
            entity.Property(e => e.IsPrimary).HasDefaultValue(false);

            entity.HasOne(d => d.PlantCombo).WithMany(p => p.PlantComboImages)
                .HasForeignKey(d => d.PlantComboId)
                .HasConstraintName("PlantComboImage_PlantComboId_fkey");
        });

        modelBuilder.Entity<PlantComboItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PlantComboItem_pkey");

            entity.ToTable("PlantComboItem");

            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.PlantCombo).WithMany(p => p.PlantComboItems)
                .HasForeignKey(d => d.PlantComboId)
                .HasConstraintName("PlantComboItem_PlantComboId_fkey");

            entity.HasOne(d => d.Plant).WithMany(p => p.PlantComboItems)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("PlantComboItem_PlantId_fkey");
        });

        modelBuilder.Entity<PlantGuide>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PlantGuide_pkey");

            entity.ToTable("PlantGuide");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Fertilizing).HasMaxLength(255);
            entity.Property(e => e.Pruning).HasMaxLength(255);
            entity.Property(e => e.Temperature).HasMaxLength(255);
            entity.Property(e => e.Watering).HasMaxLength(255);

            entity.HasOne(d => d.Plant).WithMany(p => p.PlantGuides)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("PlantGuide_PlantId_fkey");
        });

        modelBuilder.Entity<PlantImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PlantImage_pkey");

            entity.ToTable("PlantImage");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ImageUrl).HasMaxLength(512);
            entity.Property(e => e.IsPrimary).HasDefaultValue(false);

            entity.HasOne(d => d.Plant).WithMany(p => p.PlantImages)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("PlantImage_PlantId_fkey");
        });

        modelBuilder.Entity<PlantInstance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PlantInstance_pkey");

            entity.ToTable("PlantInstance");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.HealthStatus).HasMaxLength(50);
            entity.Property(e => e.SpecificPrice).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Plant).WithMany(p => p.PlantInstances)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("PlantInstance_PlantId_fkey");
        });

        modelBuilder.Entity<PlantRating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PlantRating_pkey");

            entity.ToTable("PlantRating");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Rating).HasPrecision(2, 1);

            entity.HasOne(d => d.Plant).WithMany(p => p.PlantRatings)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("PlantRating_PlantId_fkey");

            entity.HasOne(d => d.PlantInstance).WithMany(p => p.PlantRatings)
                .HasForeignKey(d => d.PlantInstanceId)
                .HasConstraintName("PlantRating_PlantInstanceId_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.PlantRatings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("PlantRating_UserId_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("RefreshToken_pkey");

            entity.ToTable("RefreshToken");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);
            entity.Property(e => e.Token).HasMaxLength(512);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("RefreshToken_UserId_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Role_pkey");

            entity.ToTable("Role");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<RoomImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("RoomImage_pkey");

            entity.ToTable("RoomImage");

            entity.Property(e => e.ImageUrl).HasMaxLength(512);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.LayoutDesign).WithMany(p => p.RoomImages)
                .HasForeignKey(d => d.LayoutDesignId)
                .HasConstraintName("FK_RoomImage_LayoutDesign");

            entity.HasOne(d => d.User).WithMany(p => p.RoomImages)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("RoomImage_UserId_fkey");
        });

        modelBuilder.Entity<RoomUploadModeration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("RoomUploadModeration_pkey");

            entity.ToTable("RoomUploadModeration");

            entity.Property(e => e.Reason).HasMaxLength(255);

            entity.HasOne(d => d.RoomImage).WithMany(p => p.RoomUploadModerations)
                .HasForeignKey(d => d.RoomImageId)
                .HasConstraintName("RoomUploadModeration_RoomImageId_fkey");
        });

        modelBuilder.Entity<ServiceProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ServiceProgress_pkey");

            entity.ToTable("ServiceProgress");

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.EvidenceImageUrl).HasMaxLength(512);

            entity.HasOne(d => d.Caretaker).WithMany(p => p.ServiceProgresses)
                .HasForeignKey(d => d.CaretakerId)
                .HasConstraintName("ServiceProgress_CaretakerId_fkey");

            entity.HasOne(d => d.ServiceRegistration).WithMany(p => p.ServiceProgresses)
                .HasForeignKey(d => d.ServiceRegistrationId)
                .HasConstraintName("ServiceProgress_ServiceRegistrationId_fkey");
        });

        modelBuilder.Entity<ServiceRating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ServiceRating_pkey");

            entity.ToTable("ServiceRating");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Rating).HasPrecision(2, 1);

            entity.HasOne(d => d.ServiceRegistration).WithMany(p => p.ServiceRatings)
                .HasForeignKey(d => d.ServiceRegistrationId)
                .HasConstraintName("ServiceRating_ServiceRegistrationId_fkey");
        });

        modelBuilder.Entity<ServiceRegistration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ServiceRegistration_pkey");

            entity.ToTable("ServiceRegistration");

            entity.Property(e => e.CancelReason).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Note).HasMaxLength(255);

            entity.HasOne(d => d.CurrentCaretaker).WithMany(p => p.ServiceRegistrationCurrentCaretakers)
                .HasForeignKey(d => d.CurrentCaretakerId)
                .HasConstraintName("ServiceRegistration_CurrentCaretakerId_fkey");

            entity.HasOne(d => d.MainCaretaker).WithMany(p => p.ServiceRegistrationMainCaretakers)
                .HasForeignKey(d => d.MainCaretakerId)
                .HasConstraintName("ServiceRegistration_MainCaretakerId_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.ServiceRegistrations)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("ServiceRegistration_OrderId_fkey");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceRegistrations)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("ServiceRegistration_ServiceId_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ServiceRegistrationUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("ServiceRegistration_UserId_fkey");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Tag_pkey");

            entity.ToTable("Tag");

            entity.Property(e => e.TagName).HasMaxLength(50);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Transaction_pkey");

            entity.ToTable("Transaction");

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.OrderInfo).HasMaxLength(255);
            entity.Property(e => e.ResponseCode).HasMaxLength(50);
            entity.Property(e => e.TransactionId).HasMaxLength(100);

            entity.HasOne(d => d.Payment).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.PaymentId)
                .HasConstraintName("Transaction_PaymentId_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("User_pkey");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "User_Email_key").IsUnique();

            entity.Property(e => e.AvatarUrl).HasMaxLength(512);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.SecurityStamp).HasMaxLength(255);
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("User_RoleId_fkey");
        });

        modelBuilder.Entity<UserBehaviorLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserBehaviorLog_pkey");

            entity.ToTable("UserBehaviorLog");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Metadata).HasColumnType("jsonb");

            entity.HasOne(d => d.PlantCombo).WithMany(p => p.UserBehaviorLogs)
                .HasForeignKey(d => d.PlantComboId)
                .HasConstraintName("UserBehaviorLog_PlantComboId_fkey");

            entity.HasOne(d => d.Plant).WithMany(p => p.UserBehaviorLogs)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("UserBehaviorLog_PlantId_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserBehaviorLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("UserBehaviorLog_UserId_fkey");
        });

        modelBuilder.Entity<UserPlant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserPlant_pkey");

            entity.ToTable("UserPlant");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.HealthStatus).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Plant).WithMany(p => p.UserPlants)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("UserPlant_PlantId_fkey");

            entity.HasOne(d => d.PlantInstance).WithMany(p => p.UserPlants)
                .HasForeignKey(d => d.PlantInstanceId)
                .HasConstraintName("UserPlant_PlantInstanceId_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserPlants)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("UserPlant_UserId_fkey");
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserPreference_pkey");

            entity.ToTable("UserPreference");

            entity.HasOne(d => d.Plant).WithMany(p => p.UserPreferences)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("UserPreference_PlantId_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserPreferences)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("UserPreference_UserId_fkey");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserProfile_pkey");

            entity.ToTable("UserProfile");

            entity.HasIndex(e => e.UserId, "UserProfile_UserId_key").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.NotificationPreferences).HasColumnType("jsonb");
            entity.Property(e => e.ReceiveNotifications).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .HasConstraintName("UserProfile_UserId_fkey");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Wishlist_pkey");

            entity.ToTable("Wishlist");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Plant).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.PlantId)
                .HasConstraintName("Wishlist_PlantId_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("Wishlist_UserId_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
