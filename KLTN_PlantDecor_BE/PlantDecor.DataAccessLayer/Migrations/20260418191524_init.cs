using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace PlantDecor.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "CareServicePackage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Features = table.Column<string>(type: "text", nullable: true),
                    VisitPerWeek = table.Column<int>(type: "integer", nullable: true),
                    DurationDays = table.Column<int>(type: "integer", nullable: true),
                    ServiceType = table.Column<int>(type: "integer", nullable: true),
                    AreaLimit = table.Column<int>(type: "integer", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("CareServicePackage_pkey", x => x.Id);
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
                    CategoryType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
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
                    StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    EndedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ChatSession_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DesignTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Style = table.Column<int>(type: "integer", nullable: true),
                    RoomTypes = table.Column<List<int>>(type: "integer[]", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("DesignTemplate_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Embeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ChunkCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Content = table.Column<string>(type: "text", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Embeddings", x => x.Id);
                    table.CheckConstraint("CK_Embeddings_ChunkCount_Positive", "\"ChunkCount\" >= 1");
                    table.CheckConstraint("CK_Embeddings_ChunkIndex_Lt_ChunkCount", "\"ChunkIndex\" < \"ChunkCount\"");
                    table.CheckConstraint("CK_Embeddings_ChunkIndex_NonNegative", "\"ChunkIndex\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Material",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaterialCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Specifications = table.Column<string>(type: "jsonb", nullable: true),
                    ExpiryMonths = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Material_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SpecificName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PlacementType = table.Column<int>(type: "integer", nullable: false),
                    RoomStyle = table.Column<List<int>>(type: "integer[]", nullable: true),
                    RoomType = table.Column<List<int>>(type: "integer[]", nullable: true),
                    Size = table.Column<int>(type: "integer", nullable: true),
                    GrowthRate = table.Column<int>(type: "integer", nullable: false),
                    Toxicity = table.Column<bool>(type: "boolean", nullable: true),
                    AirPurifying = table.Column<bool>(type: "boolean", nullable: true),
                    HasFlower = table.Column<bool>(type: "boolean", nullable: true),
                    FengShuiElement = table.Column<int>(type: "integer", nullable: true),
                    FengShuiMeaning = table.Column<string>(type: "text", nullable: true),
                    PotIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    PotSize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsUniqueInstance = table.Column<bool>(type: "boolean", nullable: false),
                    PetSafe = table.Column<bool>(type: "boolean", nullable: true),
                    ChildSafe = table.Column<bool>(type: "boolean", nullable: true),
                    CareLevelType = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
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
                    SuitableSpace = table.Column<int>(type: "integer", nullable: true),
                    SuitableRooms = table.Column<List<int>>(type: "integer[]", nullable: true),
                    FengShuiElement = table.Column<int>(type: "integer", nullable: true),
                    FengShuiPurpose = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PetSafe = table.Column<bool>(type: "boolean", nullable: true),
                    ChildSafe = table.Column<bool>(type: "boolean", nullable: true),
                    ThemeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ThemeDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ComboPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Season = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    PurchaseCount = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
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
                name: "Shift",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftName = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Shift_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specialization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Specialization_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TagType = table.Column<int>(type: "integer", nullable: false)
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
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
                name: "DesignTemplateTier",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DesignTemplateId = table.Column<int>(type: "integer", nullable: false),
                    TierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MinArea = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxArea = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PackagePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ScopedOfWork = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EstimatedDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("DesignTemplateTier_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "DesignTemplateTier_DesignTemplateId_fkey",
                        column: x => x.DesignTemplateId,
                        principalTable: "DesignTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialCategory",
                columns: table => new
                {
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("MaterialCategory_pkey", x => new { x.MaterialId, x.CategoryId });
                    table.ForeignKey(
                        name: "MaterialCategory_CategoryId_fkey",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "MaterialCategory_MaterialId_fkey",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaterialImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaterialId = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("MaterialImage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "MaterialImage_MaterialId_fkey",
                        column: x => x.MaterialId,
                        principalTable: "Material",
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
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    LightRequirement = table.Column<int>(type: "integer", nullable: true),
                    Watering = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Fertilizing = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Pruning = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Temperature = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Humidity = table.Column<string>(type: "text", nullable: true),
                    Soil = table.Column<string>(type: "text", nullable: true),
                    CareNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantGuide_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantGuide_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
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
                name: "CareServiceSpecialization",
                columns: table => new
                {
                    PackageId = table.Column<int>(type: "integer", nullable: false),
                    SpecializationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("CareServiceSpecialization_pkey", x => new { x.SpecializationId, x.PackageId });
                    table.ForeignKey(
                        name: "CareServiceSpecialization_PackageId_fkey",
                        column: x => x.PackageId,
                        principalTable: "CareServicePackage",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "CareServiceSpecialization_SpecializationId_fkey",
                        column: x => x.SpecializationId,
                        principalTable: "Specialization",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DesignTemplateSpecialization",
                columns: table => new
                {
                    DesignTemplateId = table.Column<int>(type: "integer", nullable: false),
                    SpecializationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DesignTemplateSpecialization_pkey", x => new { x.SpecializationId, x.DesignTemplateId });
                    table.ForeignKey(
                        name: "DesignTemplateSpecialization_DesignTemplateId_fkey",
                        column: x => x.DesignTemplateId,
                        principalTable: "DesignTemplate",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "DesignTemplateSpecialization_SpecializationId_fkey",
                        column: x => x.SpecializationId,
                        principalTable: "Specialization",
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
                name: "MaterialTag",
                columns: table => new
                {
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("MaterialTag_pkey", x => new { x.MaterialId, x.TagId });
                    table.ForeignKey(
                        name: "MaterialTag_MaterialId_fkey",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "MaterialTag_TagId_fkey",
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
                name: "DesignTemplateTierItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DesignTemplateTierId = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    MaterialId1 = table.Column<int>(type: "integer", nullable: true),
                    PlantId1 = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DesignTemplateTierItem_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "DesignTemplateTierItem_DesignTemplateTierId_fkey",
                        column: x => x.DesignTemplateTierId,
                        principalTable: "DesignTemplateTier",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "DesignTemplateTierItem_MaterialId_fkey",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "DesignTemplateTierItem_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DesignTemplateTierItem_Material_MaterialId1",
                        column: x => x.MaterialId1,
                        principalTable: "Material",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DesignTemplateTierItem_Plant_PlantId1",
                        column: x => x.PlantId1,
                        principalTable: "Plant",
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("CareReminder_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cart",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Cart_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CartItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<int>(type: "integer", nullable: true),
                    CommonPlantId = table.Column<int>(type: "integer", nullable: true),
                    NurseryPlantComboId = table.Column<int>(type: "integer", nullable: true),
                    NurseryMaterialId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true, defaultValue: 1),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("CartItem_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "CartItem_CartId_fkey",
                        column: x => x.CartId,
                        principalTable: "Cart",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChatParticipant",
                columns: table => new
                {
                    ChatSessionId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatParticipant", x => new { x.ChatSessionId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ChatParticipant_ChatSession_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "ChatSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommonPlant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("CommonPlant_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "CommonPlant_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CustomerSurvey",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    HasPets = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    HasChildren = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    MaxBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ExperienceLevel = table.Column<int>(type: "integer", nullable: false),
                    PreferredPlacement = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("CustomerSurvey_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DesignRegistration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
                    DesignTemplateTierId = table.Column<int>(type: "integer", nullable: false),
                    AssignedCaretakerId = table.Column<int>(type: "integer", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    Width = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Length = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CurrentStateImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DesignRegistration_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "DesignRegistration_DesignTemplateTierId_fkey",
                        column: x => x.DesignTemplateTierId,
                        principalTable: "DesignTemplateTier",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DesignTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DesignRegistrationId = table.Column<int>(type: "integer", nullable: false),
                    AssignedStaffId = table.Column<int>(type: "integer", nullable: true),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TaskType = table.Column<int>(type: "integer", nullable: false),
                    ReportImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DesignTask_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "DesignTask_DesignRegistrationId_fkey",
                        column: x => x.DesignRegistrationId,
                        principalTable: "DesignRegistration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskMaterialUsage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DesignTaskId = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("TaskMaterialUsage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "TaskMaterialUsage_DesignTaskId_fkey",
                        column: x => x.DesignTaskId,
                        principalTable: "DesignTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "TaskMaterialUsage_MaterialId_fkey",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    IssuedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    CustomerEmail = table.Column<string>(type: "text", nullable: true),
                    CustomerAddress = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Invoice_pkey", x => x.Id);
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
                name: "LayoutDesign",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    RoomImageId = table.Column<int>(type: "integer", nullable: true),
                    PreviewImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    RawResponse = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    IsSaved = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("LayoutDesign_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LayoutDesignAIResponseImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LayoutDesignId = table.Column<int>(type: "integer", nullable: false),
                    LayoutDesignPlantId = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PublicId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FluxPromptUsed = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("LayoutDesignAIResponseImage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "LayoutDesignAIResponseImage_LayoutDesignId_fkey",
                        column: x => x.LayoutDesignId,
                        principalTable: "LayoutDesign",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LayoutDesignPlant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LayoutDesignId = table.Column<int>(type: "integer", nullable: false),
                    CommonPlantId = table.Column<int>(type: "integer", nullable: true),
                    PlantInstanceId = table.Column<int>(type: "integer", nullable: true),
                    PlantReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PlacementPosition = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PlacementReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("LayoutDesignPlant_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "LayoutDesignPlant_CommonPlantId_fkey",
                        column: x => x.CommonPlantId,
                        principalTable: "CommonPlant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "LayoutDesignPlant_LayoutDesignId_fkey",
                        column: x => x.LayoutDesignId,
                        principalTable: "LayoutDesign",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Area = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Nursery_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NurseryCareService",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CareServicePackageId = table.Column<int>(type: "integer", nullable: false),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("NurseryCareService_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "NurseryCareService_CareServicePackageId_fkey",
                        column: x => x.CareServicePackageId,
                        principalTable: "CareServicePackage",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "NurseryCareService_NurseryId_fkey",
                        column: x => x.NurseryId,
                        principalTable: "Nursery",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NurseryDesignTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
                    DesignTemplateId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("NurseryDesignTemplate_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "NurseryDesignTemplate_DesignTemplateId_fkey",
                        column: x => x.DesignTemplateId,
                        principalTable: "DesignTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "NurseryDesignTemplate_NurseryId_fkey",
                        column: x => x.NurseryId,
                        principalTable: "Nursery",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NurseryMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ExpiredDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("NurseryMaterial_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "NurseryMaterial_MaterialId_fkey",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "NurseryMaterial_NurseryId_fkey",
                        column: x => x.NurseryId,
                        principalTable: "Nursery",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NurseryPlantCombo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantComboId = table.Column<int>(type: "integer", nullable: false),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("NurseryPlantCombo_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "NurseryPlantCombo_NurseryId_fkey",
                        column: x => x.NurseryId,
                        principalTable: "Nursery",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "NurseryPlantCombo_PlantComboId_fkey",
                        column: x => x.PlantComboId,
                        principalTable: "PlantCombo",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantInstance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    CurrentNurseryId = table.Column<int>(type: "integer", nullable: true),
                    SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SpecificPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Height = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    TrunkDiameter = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    HealthStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Age = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantInstance_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantInstance_NurseryId_fkey",
                        column: x => x.CurrentNurseryId,
                        principalTable: "Nursery",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "PlantInstance_PlantId_fkey",
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
                    NurseryId = table.Column<int>(type: "integer", nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true, defaultValue: 1),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    SecurityStamp = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("User_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "User_NurseryId_fkey",
                        column: x => x.NurseryId,
                        principalTable: "Nursery",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "User_RoleId_fkey",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlantImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    PlantInstanceId = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantImage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantImage_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "PlantImage_PlantInstance_fkey",
                        column: x => x.PlantInstanceId,
                        principalTable: "PlantInstance",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
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
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    OrderType = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Order_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Order_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
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
                name: "RoomImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("RoomImage_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "RoomImage_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StaffSpecialization",
                columns: table => new
                {
                    StaffId = table.Column<int>(type: "integer", nullable: false),
                    SpecializationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("StaffSpecialization_pkey", x => new { x.StaffId, x.SpecializationId });
                    table.ForeignKey(
                        name: "StaffSpecialization_SpecializationId_fkey",
                        column: x => x.SpecializationId,
                        principalTable: "Specialization",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "StaffSpecialization_StaffId_fkey",
                        column: x => x.StaffId,
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
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
                    CurrentTrunkDiameter = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CurrentHeight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    HealthStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Age = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
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
                    PreferenceScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ProfileMatchScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    BehaviorScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    PurchaseHistoryScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
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
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    ReceiveNotifications = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    NotificationPreferences = table.Column<string>(type: "jsonb", nullable: true),
                    ProfileCompleteness = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
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
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: true),
                    PlantInstanceId = table.Column<int>(type: "integer", nullable: true),
                    PlantComboId = table.Column<int>(type: "integer", nullable: true),
                    MaterialId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wishlist", x => x.Id);
                    table.ForeignKey(
                        name: "Wishlist_MaterialId_fkey",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Wishlist_PlantComboId_fkey",
                        column: x => x.PlantComboId,
                        principalTable: "PlantCombo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Wishlist_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Wishlist_PlantInstanceId_fkey",
                        column: x => x.PlantInstanceId,
                        principalTable: "PlantInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Wishlist_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NurseryOrder",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
                    ShipperId = table.Column<int>(type: "integer", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ShippingStartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SubTotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DepositAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    RemainingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentStrategy = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShipperNote = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DeliveryNote = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("NurseryOrder_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "NurseryOrder_NurseryId_fkey",
                        column: x => x.NurseryId,
                        principalTable: "Nursery",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "NurseryOrder_OrderId_fkey",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "NurseryOrder_ShipperId_fkey",
                        column: x => x.ShipperId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    PaymentType = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    PaidAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Payment_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "Payment_InvoiceId_fkey",
                        column: x => x.InvoiceId,
                        principalTable: "Invoice",
                        principalColumn: "Id");
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
                    NurseryCareServiceId = table.Column<int>(type: "integer", nullable: true),
                    MainCaretakerId = table.Column<int>(type: "integer", nullable: true),
                    CurrentCaretakerId = table.Column<int>(type: "integer", nullable: true),
                    PreferredShiftId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: true),
                    ScheduleDaysOfWeek = table.Column<string>(type: "jsonb", nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TotalSessions = table.Column<int>(type: "integer", nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
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
                        name: "ServiceRegistration_NurseryCareServiceId_fkey",
                        column: x => x.NurseryCareServiceId,
                        principalTable: "NurseryCareService",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "ServiceRegistration_OrderId_fkey",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "ServiceRegistration_PrefferedShiftId_fkey",
                        column: x => x.PreferredShiftId,
                        principalTable: "Shift",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "ServiceRegistration_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoomDesignPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomImageId = table.Column<int>(type: "integer", nullable: false),
                    RoomType = table.Column<int>(type: "integer", nullable: true),
                    RoomStyle = table.Column<int>(type: "integer", nullable: true),
                    RoomArea = table.Column<int>(type: "integer", nullable: true),
                    MinBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CareLevel = table.Column<int>(type: "integer", nullable: true),
                    IsOftenAway = table.Column<bool>(type: "boolean", nullable: true),
                    NaturalLightLevel = table.Column<int>(type: "integer", nullable: true),
                    HasAllergy = table.Column<bool>(type: "boolean", nullable: true),
                    AllergyNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("RoomDesignPreferences_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "RoomDesignPreferences_RoomImageId_fkey",
                        column: x => x.RoomImageId,
                        principalTable: "RoomImage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "NurseryOrderDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NurseryOrderId = table.Column<int>(type: "integer", nullable: false),
                    CommonPlantId = table.Column<int>(type: "integer", nullable: true),
                    PlantInstanceId = table.Column<int>(type: "integer", nullable: true),
                    NurseryPlantComboId = table.Column<int>(type: "integer", nullable: true),
                    NurseryMaterialId = table.Column<int>(type: "integer", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("NurseryOrderDetail_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "NurseryOrderDetail_CommonPlantId_fkey",
                        column: x => x.CommonPlantId,
                        principalTable: "CommonPlant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "NurseryOrderDetail_NurseryMaterialId_fkey",
                        column: x => x.NurseryMaterialId,
                        principalTable: "NurseryMaterial",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "NurseryOrderDetail_NurseryOrderId_fkey",
                        column: x => x.NurseryOrderId,
                        principalTable: "NurseryOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "NurseryOrderDetail_NurseryPlantComboId_fkey",
                        column: x => x.NurseryPlantComboId,
                        principalTable: "NurseryPlantCombo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "NurseryOrderDetail_PlantInstanceId_fkey",
                        column: x => x.PlantInstanceId,
                        principalTable: "PlantInstance",
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP"),
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
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    EvidenceImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TaskDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CustomerNote = table.Column<string>(type: "text", nullable: true),
                    HasIncidents = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IncidentImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IncidentReason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
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
                    table.ForeignKey(
                        name: "ServiceProgress_ShiftId_fkey",
                        column: x => x.ShiftId,
                        principalTable: "Shift",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRating",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceRegistrationId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ServiceRating_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "ServiceRating_ServiceRegistrationId_fkey",
                        column: x => x.ServiceRegistrationId,
                        principalTable: "ServiceRegistration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "ServiceRating_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantRating",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PlantInstanceId = table.Column<int>(type: "integer", nullable: true),
                    NurseryOrderDetailId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "LOCALTIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantRating_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantRating_NurseryOrderDetailId_fkey",
                        column: x => x.NurseryOrderDetailId,
                        principalTable: "NurseryOrderDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "PlantRating_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "PlantRating_PlantInstanceId_fkey",
                        column: x => x.PlantInstanceId,
                        principalTable: "PlantInstance",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "PlantRating_UserId_fkey",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_CareServiceSpecialization_PackageId",
                table: "CareServiceSpecialization",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Cart_UserId",
                table: "Cart",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_CartId",
                table: "CartItem",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_CommonPlantId",
                table: "CartItem",
                column: "CommonPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_NurseryMaterialId",
                table: "CartItem",
                column: "NurseryMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_NurseryPlantComboId",
                table: "CartItem",
                column: "NurseryPlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_ParentCategoryId",
                table: "Category",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_ChatSessionId",
                table: "ChatMessage",
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
                name: "IX_CommonPlant_NurseryId",
                table: "CommonPlant",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_CommonPlant_PlantId",
                table: "CommonPlant",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSurvey_UserId",
                table: "CustomerSurvey",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DesignRegistration_AssignedCaretakerId",
                table: "DesignRegistration",
                column: "AssignedCaretakerId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignRegistration_DesignTemplateTierId",
                table: "DesignRegistration",
                column: "DesignTemplateTierId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignRegistration_NurseryId",
                table: "DesignRegistration",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignRegistration_OrderId",
                table: "DesignRegistration",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DesignRegistration_UserId",
                table: "DesignRegistration",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTask_AssignedStaffId",
                table: "DesignTask",
                column: "AssignedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTask_DesignRegistrationId",
                table: "DesignTask",
                column: "DesignRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplateSpecialization_DesignTemplateId",
                table: "DesignTemplateSpecialization",
                column: "DesignTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplateTier_DesignTemplateId",
                table: "DesignTemplateTier",
                column: "DesignTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplateTierItem_DesignTemplateTierId",
                table: "DesignTemplateTierItem",
                column: "DesignTemplateTierId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplateTierItem_MaterialId",
                table: "DesignTemplateTierItem",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplateTierItem_MaterialId1",
                table: "DesignTemplateTierItem",
                column: "MaterialId1");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplateTierItem_PlantId",
                table: "DesignTemplateTierItem",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplateTierItem_PlantId1",
                table: "DesignTemplateTierItem",
                column: "PlantId1");

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_EntityType",
                table: "Embeddings",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_Embeddings_EntityType_EntityId_ChunkIndex",
                table: "Embeddings",
                columns: new[] { "EntityType", "EntityId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_OrderId",
                table: "Invoice",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDetail_InvoiceId",
                table: "InvoiceDetail",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutDesign_RoomImageId",
                table: "LayoutDesign",
                column: "RoomImageId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutDesign_UserId",
                table: "LayoutDesign",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutDesignAIResponseImage_LayoutDesignId",
                table: "LayoutDesignAIResponseImage",
                column: "LayoutDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutDesignAIResponseImage_LayoutDesignPlantId",
                table: "LayoutDesignAIResponseImage",
                column: "LayoutDesignPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutDesignPlant_CommonPlantId",
                table: "LayoutDesignPlant",
                column: "CommonPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutDesignPlant_LayoutDesignId",
                table: "LayoutDesignPlant",
                column: "LayoutDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_LayoutDesignPlant_PlantInstanceId",
                table: "LayoutDesignPlant",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCategory_CategoryId",
                table: "MaterialCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialImage_MaterialId",
                table: "MaterialImage",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialTag_TagId",
                table: "MaterialTag",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Nursery_ManagerId",
                table: "Nursery",
                column: "ManagerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NurseryCareService_CareServicePackageId",
                table: "NurseryCareService",
                column: "CareServicePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryCareService_NurseryId",
                table: "NurseryCareService",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryDesignTemplate_DesignTemplateId",
                table: "NurseryDesignTemplate",
                column: "DesignTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryDesignTemplate_NurseryId_DesignTemplateId",
                table: "NurseryDesignTemplate",
                columns: new[] { "NurseryId", "DesignTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NurseryMaterial_MaterialId",
                table: "NurseryMaterial",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryMaterial_NurseryId",
                table: "NurseryMaterial",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryOrder_NurseryId",
                table: "NurseryOrder",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryOrder_OrderId",
                table: "NurseryOrder",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryOrder_ShipperId",
                table: "NurseryOrder",
                column: "ShipperId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryOrder_Status",
                table: "NurseryOrder",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryOrderDetail_CommonPlantId",
                table: "NurseryOrderDetail",
                column: "CommonPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryOrderDetail_NurseryMaterialId",
                table: "NurseryOrderDetail",
                column: "NurseryMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryOrderDetail_NurseryOrderId",
                table: "NurseryOrderDetail",
                column: "NurseryOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryOrderDetail_NurseryPlantComboId",
                table: "NurseryOrderDetail",
                column: "NurseryPlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryOrderDetail_PlantInstanceId",
                table: "NurseryOrderDetail",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryPlantCombo_NurseryId",
                table: "NurseryPlantCombo",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryPlantCombo_PlantComboId",
                table: "NurseryPlantCombo",
                column: "PlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Status",
                table: "Order",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Order_UserId",
                table: "Order",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_InvoiceId",
                table: "Payment",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_OrderId",
                table: "Payment",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Plant_Name",
                table: "Plant",
                column: "Name");

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
                column: "PlantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlantImage_PlantId",
                table: "PlantImage",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantImage_PlantInstanceId",
                table: "PlantImage",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantInstance_CurrentNurseryId",
                table: "PlantInstance",
                column: "CurrentNurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantInstance_PlantId",
                table: "PlantInstance",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantRating_NurseryOrderDetailId",
                table: "PlantRating",
                column: "NurseryOrderDetailId",
                unique: true);

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
                name: "IX_RoomDesignPreferences_RoomImageId",
                table: "RoomDesignPreferences",
                column: "RoomImageId");

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
                name: "IX_ServiceProgress_ShiftId",
                table: "ServiceProgress",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRating_ServiceRegistrationId",
                table: "ServiceRating",
                column: "ServiceRegistrationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRating_UserId",
                table: "ServiceRating",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_CurrentCaretakerId",
                table: "ServiceRegistration",
                column: "CurrentCaretakerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_MainCaretakerId",
                table: "ServiceRegistration",
                column: "MainCaretakerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_NurseryCareServiceId",
                table: "ServiceRegistration",
                column: "NurseryCareServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_OrderId",
                table: "ServiceRegistration",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_PreferredShiftId",
                table: "ServiceRegistration",
                column: "PreferredShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_UserId",
                table: "ServiceRegistration",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffSpecialization_SpecializationId",
                table: "StaffSpecialization",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskMaterialUsage_DesignTaskId",
                table: "TaskMaterialUsage",
                column: "DesignTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskMaterialUsage_MaterialId",
                table: "TaskMaterialUsage",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_PaymentId",
                table: "Transaction",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_User_NurseryId",
                table: "User",
                column: "NurseryId");

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
                column: "PlantInstanceId",
                unique: true);

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
                name: "IX_Wishlist_MaterialId",
                table: "Wishlist",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_PlantComboId",
                table: "Wishlist",
                column: "PlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_PlantId",
                table: "Wishlist",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_PlantInstanceId",
                table: "Wishlist",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_UserId_IsDeleted",
                table: "Wishlist",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.AddForeignKey(
                name: "AILayoutResponseModeration_LayoutDesignId_fkey",
                table: "AILayoutResponseModeration",
                column: "LayoutDesignId",
                principalTable: "LayoutDesign",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "CareReminder_UserPlantId_fkey",
                table: "CareReminder",
                column: "UserPlantId",
                principalTable: "UserPlant",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "Cart_UserId_fkey",
                table: "Cart",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "CartItem_CommonPlantId_fkey",
                table: "CartItem",
                column: "CommonPlantId",
                principalTable: "CommonPlant",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "CartItem_NurseryMaterialId_fkey",
                table: "CartItem",
                column: "NurseryMaterialId",
                principalTable: "NurseryMaterial",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "CartItem_NurseryPlantComboId_fkey",
                table: "CartItem",
                column: "NurseryPlantComboId",
                principalTable: "NurseryPlantCombo",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatParticipant_User_UserId",
                table: "ChatParticipant",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "CommonPlant_NurseryId_fkey",
                table: "CommonPlant",
                column: "NurseryId",
                principalTable: "Nursery",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "CustomerSurvey_UserId_fkey",
                table: "CustomerSurvey",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "DesignRegistration_AssignedCaretakerId_fkey",
                table: "DesignRegistration",
                column: "AssignedCaretakerId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "DesignRegistration_UserId_fkey",
                table: "DesignRegistration",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "DesignRegistration_NurseryId_fkey",
                table: "DesignRegistration",
                column: "NurseryId",
                principalTable: "Nursery",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "DesignRegistration_OrderId_fkey",
                table: "DesignRegistration",
                column: "OrderId",
                principalTable: "Order",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "DesignTask_AssignedStaffId_fkey",
                table: "DesignTask",
                column: "AssignedStaffId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "Invoice_OrderId_fkey",
                table: "Invoice",
                column: "OrderId",
                principalTable: "Order",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "LayoutDesign_RoomImageId_fkey",
                table: "LayoutDesign",
                column: "RoomImageId",
                principalTable: "RoomImage",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "LayoutDesign_UserId_fkey",
                table: "LayoutDesign",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "LayoutDesignAIResponseImage_LayoutDesignPlantId_fkey",
                table: "LayoutDesignAIResponseImage",
                column: "LayoutDesignPlantId",
                principalTable: "LayoutDesignPlant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "LayoutDesignPlant_PlantInstanceId_fkey",
                table: "LayoutDesignPlant",
                column: "PlantInstanceId",
                principalTable: "PlantInstance",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "Nursery_ManagerId_fkey",
                table: "Nursery",
                column: "ManagerId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Nursery_ManagerId_fkey",
                table: "Nursery");

            migrationBuilder.DropTable(
                name: "AILayoutResponseModeration");

            migrationBuilder.DropTable(
                name: "CareReminder");

            migrationBuilder.DropTable(
                name: "CareServiceSpecialization");

            migrationBuilder.DropTable(
                name: "CartItem");

            migrationBuilder.DropTable(
                name: "ChatMessage");

            migrationBuilder.DropTable(
                name: "ChatParticipant");

            migrationBuilder.DropTable(
                name: "ComboTag");

            migrationBuilder.DropTable(
                name: "CustomerSurvey");

            migrationBuilder.DropTable(
                name: "DesignTemplateSpecialization");

            migrationBuilder.DropTable(
                name: "DesignTemplateTierItem");

            migrationBuilder.DropTable(
                name: "Embeddings");

            migrationBuilder.DropTable(
                name: "InvoiceDetail");

            migrationBuilder.DropTable(
                name: "LayoutDesignAIResponseImage");

            migrationBuilder.DropTable(
                name: "MaterialCategory");

            migrationBuilder.DropTable(
                name: "MaterialImage");

            migrationBuilder.DropTable(
                name: "MaterialTag");

            migrationBuilder.DropTable(
                name: "NurseryDesignTemplate");

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
                name: "RoomDesignPreferences");

            migrationBuilder.DropTable(
                name: "RoomUploadModeration");

            migrationBuilder.DropTable(
                name: "ServiceProgress");

            migrationBuilder.DropTable(
                name: "ServiceRating");

            migrationBuilder.DropTable(
                name: "StaffSpecialization");

            migrationBuilder.DropTable(
                name: "TaskMaterialUsage");

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
                name: "LayoutDesignPlant");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "NurseryOrderDetail");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropTable(
                name: "ServiceRegistration");

            migrationBuilder.DropTable(
                name: "Specialization");

            migrationBuilder.DropTable(
                name: "DesignTask");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "LayoutDesign");

            migrationBuilder.DropTable(
                name: "CommonPlant");

            migrationBuilder.DropTable(
                name: "NurseryMaterial");

            migrationBuilder.DropTable(
                name: "NurseryOrder");

            migrationBuilder.DropTable(
                name: "NurseryPlantCombo");

            migrationBuilder.DropTable(
                name: "PlantInstance");

            migrationBuilder.DropTable(
                name: "NurseryCareService");

            migrationBuilder.DropTable(
                name: "Shift");

            migrationBuilder.DropTable(
                name: "DesignRegistration");

            migrationBuilder.DropTable(
                name: "Invoice");

            migrationBuilder.DropTable(
                name: "RoomImage");

            migrationBuilder.DropTable(
                name: "Material");

            migrationBuilder.DropTable(
                name: "PlantCombo");

            migrationBuilder.DropTable(
                name: "Plant");

            migrationBuilder.DropTable(
                name: "CareServicePackage");

            migrationBuilder.DropTable(
                name: "DesignTemplateTier");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "DesignTemplate");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Nursery");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
