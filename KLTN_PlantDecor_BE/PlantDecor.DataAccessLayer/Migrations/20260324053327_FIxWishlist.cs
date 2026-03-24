using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlantDecor.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class FIxWishlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoomImage_LayoutDesign",
                table: "RoomImage");

            migrationBuilder.DropForeignKey(
                name: "Wishlist_PlantId_fkey",
                table: "Wishlist");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "Shipping");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Wishlist",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_Wishlist_PlantId",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_RoomImage_LayoutDesignId",
                table: "RoomImage");

            migrationBuilder.DropColumn(
                name: "LayoutDesignId",
                table: "RoomImage");

            migrationBuilder.RenameColumn(
                name: "PlantId",
                table: "Wishlist",
                newName: "ItemType");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Wishlist",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "CommonPlantId",
                table: "Wishlist",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Wishlist",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Wishlist",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NurseryMaterialId",
                table: "Wishlist",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NurseryPlantComboId",
                table: "Wishlist",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlantInstanceId",
                table: "Wishlist",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AIResponseImageUrl",
                table: "LayoutDesign",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FluxPromptUsed",
                table: "LayoutDesign",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlantCollageUrl",
                table: "LayoutDesign",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawResponse",
                table: "LayoutDesign",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoomImageId",
                table: "LayoutDesign",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerAddress",
                table: "Invoice",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Invoice",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Invoice",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Wishlist",
                table: "Wishlist",
                column: "Id");

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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
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
                    table.ForeignKey(
                        name: "LayoutDesignPlant_PlantInstanceId_fkey",
                        column: x => x.PlantInstanceId,
                        principalTable: "PlantInstance",
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

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_CommonPlantId",
                table: "Wishlist",
                column: "CommonPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_NurseryMaterialId",
                table: "Wishlist",
                column: "NurseryMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_NurseryPlantComboId",
                table: "Wishlist",
                column: "NurseryPlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_PlantInstanceId",
                table: "Wishlist",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_UserId_IsDeleted",
                table: "Wishlist",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_LayoutDesign_RoomImageId",
                table: "LayoutDesign",
                column: "RoomImageId");

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
                name: "IX_RoomDesignPreferences_RoomImageId",
                table: "RoomDesignPreferences",
                column: "RoomImageId");

            migrationBuilder.AddForeignKey(
                name: "LayoutDesign_RoomImageId_fkey",
                table: "LayoutDesign",
                column: "RoomImageId",
                principalTable: "RoomImage",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "Wishlist_CommonPlantId_fkey",
                table: "Wishlist",
                column: "CommonPlantId",
                principalTable: "CommonPlant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "Wishlist_NurseryMaterialId_fkey",
                table: "Wishlist",
                column: "NurseryMaterialId",
                principalTable: "NurseryMaterial",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "Wishlist_NurseryPlantComboId_fkey",
                table: "Wishlist",
                column: "NurseryPlantComboId",
                principalTable: "NurseryPlantCombo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "Wishlist_PlantInstanceId_fkey",
                table: "Wishlist",
                column: "PlantInstanceId",
                principalTable: "PlantInstance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "LayoutDesign_RoomImageId_fkey",
                table: "LayoutDesign");

            migrationBuilder.DropForeignKey(
                name: "Wishlist_CommonPlantId_fkey",
                table: "Wishlist");

            migrationBuilder.DropForeignKey(
                name: "Wishlist_NurseryMaterialId_fkey",
                table: "Wishlist");

            migrationBuilder.DropForeignKey(
                name: "Wishlist_NurseryPlantComboId_fkey",
                table: "Wishlist");

            migrationBuilder.DropForeignKey(
                name: "Wishlist_PlantInstanceId_fkey",
                table: "Wishlist");

            migrationBuilder.DropTable(
                name: "LayoutDesignPlant");

            migrationBuilder.DropTable(
                name: "RoomDesignPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Wishlist",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_Wishlist_CommonPlantId",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_Wishlist_NurseryMaterialId",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_Wishlist_NurseryPlantComboId",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_Wishlist_PlantInstanceId",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_Wishlist_UserId_IsDeleted",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_LayoutDesign_RoomImageId",
                table: "LayoutDesign");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Wishlist");

            migrationBuilder.DropColumn(
                name: "CommonPlantId",
                table: "Wishlist");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Wishlist");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Wishlist");

            migrationBuilder.DropColumn(
                name: "NurseryMaterialId",
                table: "Wishlist");

            migrationBuilder.DropColumn(
                name: "NurseryPlantComboId",
                table: "Wishlist");

            migrationBuilder.DropColumn(
                name: "PlantInstanceId",
                table: "Wishlist");

            migrationBuilder.DropColumn(
                name: "AIResponseImageUrl",
                table: "LayoutDesign");

            migrationBuilder.DropColumn(
                name: "FluxPromptUsed",
                table: "LayoutDesign");

            migrationBuilder.DropColumn(
                name: "PlantCollageUrl",
                table: "LayoutDesign");

            migrationBuilder.DropColumn(
                name: "RawResponse",
                table: "LayoutDesign");

            migrationBuilder.DropColumn(
                name: "RoomImageId",
                table: "LayoutDesign");

            migrationBuilder.DropColumn(
                name: "CustomerAddress",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Invoice");

            migrationBuilder.RenameColumn(
                name: "ItemType",
                table: "Wishlist",
                newName: "PlantId");

            migrationBuilder.AddColumn<int>(
                name: "LayoutDesignId",
                table: "RoomImage",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Wishlist",
                table: "Wishlist",
                columns: new[] { "UserId", "PlantId" });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommonPlantId = table.Column<int>(type: "integer", nullable: true),
                    NurseryMaterialId = table.Column<int>(type: "integer", nullable: true),
                    NurseryPlantComboId = table.Column<int>(type: "integer", nullable: true),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    PlantInstanceId = table.Column<int>(type: "integer", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("OrderItem_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "OrderItem_CommonPlantId_fkey",
                        column: x => x.CommonPlantId,
                        principalTable: "CommonPlant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "OrderItem_NurseryMaterialId_fkey",
                        column: x => x.NurseryMaterialId,
                        principalTable: "NurseryMaterial",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "OrderItem_NurseryPlantComboId_fkey",
                        column: x => x.NurseryPlantComboId,
                        principalTable: "NurseryPlantCombo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "OrderItem_OrderId_fkey",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "OrderItem_PlantInstanceId_fkey",
                        column: x => x.PlantInstanceId,
                        principalTable: "PlantInstance",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Shipping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    TrackingCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Shipping_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "Shipping_OrderId_fkey",
                        column: x => x.OrderId,
                        principalTable: "Order",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_PlantId",
                table: "Wishlist",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomImage_LayoutDesignId",
                table: "RoomImage",
                column: "LayoutDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_CommonPlantId",
                table: "OrderItem",
                column: "CommonPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_NurseryMaterialId",
                table: "OrderItem",
                column: "NurseryMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_NurseryPlantComboId",
                table: "OrderItem",
                column: "NurseryPlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItem",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_PlantInstanceId",
                table: "OrderItem",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipping_OrderId",
                table: "Shipping",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoomImage_LayoutDesign",
                table: "RoomImage",
                column: "LayoutDesignId",
                principalTable: "LayoutDesign",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "Wishlist_PlantId_fkey",
                table: "Wishlist",
                column: "PlantId",
                principalTable: "Plant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
