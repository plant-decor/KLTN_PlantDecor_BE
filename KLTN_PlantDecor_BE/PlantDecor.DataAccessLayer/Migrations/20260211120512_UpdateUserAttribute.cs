using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlantDecor.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAttribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareServicePackage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentServiceId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Features = table.Column<string>(type: "text", nullable: true),
                    Frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DurationDays = table.Column<int>(type: "integer", nullable: true),
                    ServiceType = table.Column<int>(type: "integer", nullable: true),
                    DifficultyLevel = table.Column<int>(type: "integer", nullable: true),
                    AreaLimit = table.Column<int>(type: "integer", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("CareServicePackage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "CareServicePackage_ParentServiceId_fkey",
                        column: x => x.ParentServiceId,
                        principalTable: "CareServicePackage",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentCategoryId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Category_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "Category_ParentCategoryId_fkey",
                        column: x => x.ParentCategoryId,
                        principalTable: "Category",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChatSession",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    EndedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ChatSession_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Specifications = table.Column<string>(type: "jsonb", nullable: true),
                    ExpiryMonths = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Inventory_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SpecificName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Placement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Size = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MinHeight = table.Column<int>(type: "integer", nullable: true),
                    MaxHeight = table.Column<int>(type: "integer", nullable: true),
                    GrowthRate = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Toxicity = table.Column<bool>(type: "boolean", nullable: true),
                    AirPurifying = table.Column<bool>(type: "boolean", nullable: true),
                    HasFlower = table.Column<bool>(type: "boolean", nullable: true),
                    FengShuiElement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FengShuiMeaning = table.Column<string>(type: "text", nullable: true),
                    PotIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    PotSize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PlantType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CareLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Texture = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Plant_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantCombo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComboCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ComboName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ComboType = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SuitableSpace = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SuitableRooms = table.Column<string>(type: "jsonb", nullable: true),
                    FengShuiElement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FengShuiPurpose = table.Column<string>(type: "text", nullable: true),
                    ThemeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ThemeDescription = table.Column<string>(type: "text", nullable: true),
                    OriginalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    SalePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    MinPlants = table.Column<int>(type: "integer", nullable: true),
                    MaxPlants = table.Column<int>(type: "integer", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    Season = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    PurchaseCount = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantCombo_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Role_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Tag_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatSessionId = table.Column<int>(type: "integer", nullable: true),
                    Sender = table.Column<int>(type: "integer", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ChatMessage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "ChatMessage_ChatSessionId_fkey",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSession",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryCategory",
                columns: table => new
                {
                    InventoryId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("InventoryCategory_pkey", x => new { x.InventoryId, x.CategoryId });
                    table.ForeignKey(
                        name: "InventoryCategory_CategoryId_fkey",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "InventoryCategory_InventoryId_fkey",
                        column: x => x.InventoryId,
                        principalTable: "Inventory",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryId = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("InventoryImage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "InventoryImage_InventoryId_fkey",
                        column: x => x.InventoryId,
                        principalTable: "Inventory",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantCategory",
                columns: table => new
                {
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantCategory_pkey", x => new { x.PlantId, x.CategoryId });
                    table.ForeignKey(
                        name: "PlantCategory_CategoryId_fkey",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "PlantCategory_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantGuide",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    LightRequirement = table.Column<int>(type: "integer", nullable: true),
                    Watering = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Fertilizing = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Pruning = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Temperature = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CareNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantGuide_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantGuide_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantImage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantImage_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantInstance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    SpecificPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Height = table.Column<decimal>(type: "numeric", nullable: true),
                    TrunkDiameter = table.Column<decimal>(type: "numeric", nullable: true),
                    HealthStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Age = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantInstance_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantInstance_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantComboImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantComboId = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantComboImage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantComboImage_PlantComboId_fkey",
                        column: x => x.PlantComboId,
                        principalTable: "PlantCombo",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantComboItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantComboId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true, defaultValue: 1),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantComboItem_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantComboItem_PlantComboId_fkey",
                        column: x => x.PlantComboId,
                        principalTable: "PlantCombo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "PlantComboItem_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true, defaultValue: 1),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    SecurityStamp = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("User_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "User_RoleId_fkey",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ComboTag",
                columns: table => new
                {
                    PlantComboId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ComboTag_pkey", x => new { x.PlantComboId, x.TagId });
                    table.ForeignKey(
                        name: "ComboTag_PlantComboId_fkey",
                        column: x => x.PlantComboId,
                        principalTable: "PlantCombo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "ComboTag_TagId_fkey",
                        column: x => x.TagId,
                        principalTable: "Tag",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTag",
                columns: table => new
                {
                    InventoryId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("InventoryTag_pkey", x => new { x.InventoryId, x.TagId });
                    table.ForeignKey(
                        name: "InventoryTag_InventoryId_fkey",
                        column: x => x.InventoryId,
                        principalTable: "Inventory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "InventoryTag_TagId_fkey",
                        column: x => x.TagId,
                        principalTable: "Tag",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantTag",
                columns: table => new
                {
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantTag_pkey", x => new { x.PlantId, x.TagId });
                    table.ForeignKey(
                        name: "PlantTag_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "PlantTag_TagId_fkey",
                        column: x => x.TagId,
                        principalTable: "Tag",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Cart",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Cart_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "Cart_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChatParticipant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatSessionId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ChatParticipant_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "ChatParticipant_ChatSessionId_fkey",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSession",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "ChatParticipant_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LayoutDesign",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    PreviewImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ModerationStatus = table.Column<int>(type: "integer", nullable: true),
                    AllowedToAll = table.Column<string>(type: "jsonb", nullable: true),
                    IsSaved = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("LayoutDesign_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "LayoutDesign_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Nursery",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ManagerId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Area = table.Column<decimal>(type: "numeric", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: true),
                    LightCondition = table.Column<int>(type: "integer", nullable: true),
                    HumidityLevel = table.Column<int>(type: "integer", nullable: true),
                    HasMistSystem = table.Column<bool>(type: "boolean", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Nursery_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "Nursery_ManagerId_fkey",
                        column: x => x.ManagerId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    RemainingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PaymentStrategy = table.Column<int>(type: "integer", nullable: true),
                    ReturnReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShipperNote = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    OrderType = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Order_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "Order_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantRating",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    PlantInstanceId = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<decimal>(type: "numeric(2,1)", precision: 2, scale: 1, nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantRating_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantRating_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "PlantRating_PlantInstanceId_fkey",
                        column: x => x.PlantInstanceId,
                        principalTable: "PlantInstance",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "PlantRating_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("RefreshToken_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "RefreshToken_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserBehaviorLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    PlantComboId = table.Column<int>(type: "integer", nullable: true),
                    ActionType = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("UserBehaviorLog_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "UserBehaviorLog_PlantComboId_fkey",
                        column: x => x.PlantComboId,
                        principalTable: "PlantCombo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "UserBehaviorLog_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "UserBehaviorLog_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserPlant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    PlantInstanceId = table.Column<int>(type: "integer", nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastWateredDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastFertilizedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastPrunedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CurrentTrunkDiameter = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentHeight = table.Column<decimal>(type: "numeric", nullable: true),
                    HealthStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Age = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("UserPlant_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "UserPlant_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "UserPlant_PlantInstanceId_fkey",
                        column: x => x.PlantInstanceId,
                        principalTable: "PlantInstance",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "UserPlant_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserPreference",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    PreferenceScore = table.Column<decimal>(type: "numeric", nullable: true),
                    ProfileMatchScore = table.Column<decimal>(type: "numeric", nullable: true),
                    BehaviorScore = table.Column<decimal>(type: "numeric", nullable: true),
                    PurchaseHistoryScore = table.Column<decimal>(type: "numeric", nullable: true),
                    LastCalculated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("UserPreference_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "UserPreference_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "UserPreference_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BirthYear = table.Column<int>(type: "integer", nullable: true),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: true),
                    ReceiveNotifications = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    NotificationPreferences = table.Column<string>(type: "jsonb", nullable: true),
                    ProfileCompleteness = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("UserProfile_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "UserProfile_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Wishlist",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Wishlist_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "Wishlist_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Wishlist_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CartItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    PlantInstanceId = table.Column<int>(type: "integer", nullable: true),
                    PlantComboId = table.Column<int>(type: "integer", nullable: true),
                    InventoryId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true, defaultValue: 1),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("CartItem_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "CartItem_CartId_fkey",
                        column: x => x.CartId,
                        principalTable: "Cart",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "CartItem_InventoryId_fkey",
                        column: x => x.InventoryId,
                        principalTable: "Inventory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "CartItem_PlantComboId_fkey",
                        column: x => x.PlantComboId,
                        principalTable: "PlantCombo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "CartItem_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "CartItem_PlantInstanceId_fkey",
                        column: x => x.PlantInstanceId,
                        principalTable: "PlantInstance",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AILayoutResponseModeration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LayoutDesignId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("AILayoutResponseModeration_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "AILayoutResponseModeration_LayoutDesignId_fkey",
                        column: x => x.LayoutDesignId,
                        principalTable: "LayoutDesign",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoomImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    LayoutDesignId = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ViewAngle = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("RoomImage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomImage_LayoutDesign",
                        column: x => x.LayoutDesignId,
                        principalTable: "LayoutDesign",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "RoomImage_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    IssuedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Invoice_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "Invoice_OrderId_fkey",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    PlantInstanceId = table.Column<int>(type: "integer", nullable: true),
                    PlantComboId = table.Column<int>(type: "integer", nullable: true),
                    InventoryId = table.Column<int>(type: "integer", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("OrderItem_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "OrderItem_InventoryId_fkey",
                        column: x => x.InventoryId,
                        principalTable: "Inventory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "OrderItem_OrderId_fkey",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "OrderItem_PlantComboId_fkey",
                        column: x => x.PlantComboId,
                        principalTable: "PlantCombo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "OrderItem_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "OrderItem_PlantInstanceId_fkey",
                        column: x => x.PlantInstanceId,
                        principalTable: "PlantInstance",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    PaymentType = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    PaidAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Payment_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "Payment_OrderId_fkey",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceRegistration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    MainCaretakerId = table.Column<int>(type: "integer", nullable: true),
                    CurrentCaretakerId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ServiceDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    EstimatedDuration = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ServiceRegistration_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "ServiceRegistration_CurrentCaretakerId_fkey",
                        column: x => x.CurrentCaretakerId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "ServiceRegistration_MainCaretakerId_fkey",
                        column: x => x.MainCaretakerId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "ServiceRegistration_OrderId_fkey",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "ServiceRegistration_ServiceId_fkey",
                        column: x => x.ServiceId,
                        principalTable: "CareServicePackage",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "ServiceRegistration_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CareReminder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserPlantId = table.Column<int>(type: "integer", nullable: true),
                    CareType = table.Column<int>(type: "integer", nullable: true),
                    Content = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReminderDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("CareReminder_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "CareReminder_UserPlantId_fkey",
                        column: x => x.UserPlantId,
                        principalTable: "UserPlant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoomUploadModeration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomImageId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("RoomUploadModeration_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "RoomUploadModeration_RoomImageId_fkey",
                        column: x => x.RoomImageId,
                        principalTable: "RoomImage",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvoiceDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("InvoiceDetail_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "InvoiceDetail_InvoiceId_fkey",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResponseCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OrderInfo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Transaction_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "Transaction_PaymentId_fkey",
                        column: x => x.PaymentId,
                        principalTable: "Payment",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceRegistrationId = table.Column<int>(type: "integer", nullable: true),
                    CaretakerId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    EvidenceImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ActualStartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ServiceProgress_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "ServiceProgress_CaretakerId_fkey",
                        column: x => x.CaretakerId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "ServiceProgress_ServiceRegistrationId_fkey",
                        column: x => x.ServiceRegistrationId,
                        principalTable: "ServiceRegistration",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceRating",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceRegistrationId = table.Column<int>(type: "integer", nullable: true),
                    Rating = table.Column<decimal>(type: "numeric(2,1)", precision: 2, scale: 1, nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ServiceRating_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "ServiceRating_ServiceRegistrationId_fkey",
                        column: x => x.ServiceRegistrationId,
                        principalTable: "ServiceRegistration",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AILayoutResponseModeration_LayoutDesignId",
                table: "AILayoutResponseModeration",
                column: "LayoutDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_CareReminder_UserPlantId",
                table: "CareReminder",
                column: "UserPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_CareServicePackage_ParentServiceId",
                table: "CareServicePackage",
                column: "ParentServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Cart_UserId",
                table: "Cart",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_CartId",
                table: "CartItem",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_InventoryId",
                table: "CartItem",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_PlantComboId",
                table: "CartItem",
                column: "PlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_PlantId",
                table: "CartItem",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_PlantInstanceId",
                table: "CartItem",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_ParentCategoryId",
                table: "Category",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_ChatSessionId",
                table: "ChatMessage",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipant_ChatSessionId",
                table: "ChatParticipant",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipant_UserId",
                table: "ChatParticipant",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ComboTag_TagId",
                table: "ComboTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategory_CategoryId",
                table: "InventoryCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryImage_InventoryId",
                table: "InventoryImage",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTag_TagId",
                table: "InventoryTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_OrderId",
                table: "Invoice",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDetail_InvoiceId",
                table: "InvoiceDetail",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutDesign_UserId",
                table: "LayoutDesign",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Nursery_ManagerId",
                table: "Nursery",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Status",
                table: "Order",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Order_UserId",
                table: "Order",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_InventoryId",
                table: "OrderItem",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItem",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_PlantComboId",
                table: "OrderItem",
                column: "PlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_PlantId",
                table: "OrderItem",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_PlantInstanceId",
                table: "OrderItem",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_OrderId",
                table: "Payment",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Plant_Name",
                table: "Plant",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "Plant_PlantCode_key",
                table: "Plant",
                column: "PlantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlantCategory_CategoryId",
                table: "PlantCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantComboImage_ComboId",
                table: "PlantComboImage",
                column: "PlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantComboItem_PlantComboId",
                table: "PlantComboItem",
                column: "PlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantComboItem_PlantId",
                table: "PlantComboItem",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantGuide_PlantId",
                table: "PlantGuide",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantImage_PlantId",
                table: "PlantImage",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantInstance_PlantId",
                table: "PlantInstance",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantRating_PlantId",
                table: "PlantRating",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantRating_PlantInstanceId",
                table: "PlantRating",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantRating_UserId",
                table: "PlantRating",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantTag_TagId",
                table: "PlantTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId",
                table: "RefreshToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomImage_LayoutDesignId",
                table: "RoomImage",
                column: "LayoutDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomImage_UserId",
                table: "RoomImage",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomUploadModeration_RoomImageId",
                table: "RoomUploadModeration",
                column: "RoomImageId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProgress_CaretakerId",
                table: "ServiceProgress",
                column: "CaretakerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProgress_ServiceRegistrationId",
                table: "ServiceProgress",
                column: "ServiceRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRating_ServiceRegistrationId",
                table: "ServiceRating",
                column: "ServiceRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_CurrentCaretakerId",
                table: "ServiceRegistration",
                column: "CurrentCaretakerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_MainCaretakerId",
                table: "ServiceRegistration",
                column: "MainCaretakerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_OrderId",
                table: "ServiceRegistration",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_ServiceId",
                table: "ServiceRegistration",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_UserId",
                table: "ServiceRegistration",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_PaymentId",
                table: "Transaction",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleId",
                table: "User",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "User_Email_key",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorLog_PlantComboId",
                table: "UserBehaviorLog",
                column: "PlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorLog_PlantId",
                table: "UserBehaviorLog",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorLog_UserId",
                table: "UserBehaviorLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlant_PlantId",
                table: "UserPlant",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlant_PlantInstanceId",
                table: "UserPlant",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlant_UserId",
                table: "UserPlant",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreference_PlantId",
                table: "UserPreference",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreference_UserId",
                table: "UserPreference",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UserProfile_UserId_key",
                table: "UserProfile",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_PlantId",
                table: "Wishlist",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_UserId",
                table: "Wishlist",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AILayoutResponseModeration");

            migrationBuilder.DropTable(
                name: "CareReminder");

            migrationBuilder.DropTable(
                name: "CartItem");

            migrationBuilder.DropTable(
                name: "ChatMessage");

            migrationBuilder.DropTable(
                name: "ChatParticipant");

            migrationBuilder.DropTable(
                name: "ComboTag");

            migrationBuilder.DropTable(
                name: "InventoryCategory");

            migrationBuilder.DropTable(
                name: "InventoryImage");

            migrationBuilder.DropTable(
                name: "InventoryTag");

            migrationBuilder.DropTable(
                name: "InvoiceDetail");

            migrationBuilder.DropTable(
                name: "Nursery");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "PlantCategory");

            migrationBuilder.DropTable(
                name: "PlantComboImage");

            migrationBuilder.DropTable(
                name: "PlantComboItem");

            migrationBuilder.DropTable(
                name: "PlantGuide");

            migrationBuilder.DropTable(
                name: "PlantImage");

            migrationBuilder.DropTable(
                name: "PlantRating");

            migrationBuilder.DropTable(
                name: "PlantTag");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "RoomUploadModeration");

            migrationBuilder.DropTable(
                name: "ServiceProgress");

            migrationBuilder.DropTable(
                name: "ServiceRating");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "UserBehaviorLog");

            migrationBuilder.DropTable(
                name: "UserPreference");

            migrationBuilder.DropTable(
                name: "UserProfile");

            migrationBuilder.DropTable(
                name: "Wishlist");

            migrationBuilder.DropTable(
                name: "UserPlant");

            migrationBuilder.DropTable(
                name: "Cart");

            migrationBuilder.DropTable(
                name: "ChatSession");

            migrationBuilder.DropTable(
                name: "Invoice");

            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropTable(
                name: "RoomImage");

            migrationBuilder.DropTable(
                name: "ServiceRegistration");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "PlantCombo");

            migrationBuilder.DropTable(
                name: "PlantInstance");

            migrationBuilder.DropTable(
                name: "LayoutDesign");

            migrationBuilder.DropTable(
                name: "CareServicePackage");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "Plant");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
