using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlantDecor.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDBforNursery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "CartItem_InventoryId_fkey",
                table: "CartItem");

            migrationBuilder.DropForeignKey(
                name: "CartItem_PlantComboId_fkey",
                table: "CartItem");

            migrationBuilder.DropForeignKey(
                name: "CartItem_PlantId_fkey",
                table: "CartItem");

            migrationBuilder.DropForeignKey(
                name: "CartItem_PlantInstanceId_fkey",
                table: "CartItem");

            migrationBuilder.DropForeignKey(
                name: "OrderItem_InventoryId_fkey",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "OrderItem_PlantComboId_fkey",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "OrderItem_PlantId_fkey",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "ServiceRegistration_ServiceId_fkey",
                table: "ServiceRegistration");

            migrationBuilder.DropTable(
                name: "InventoryCategory");

            migrationBuilder.DropTable(
                name: "InventoryImage");

            migrationBuilder.DropTable(
                name: "InventoryTag");

            migrationBuilder.DropTable(
                name: "PlantInventory");

            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRegistration_OrderId",
                table: "ServiceRegistration");

            migrationBuilder.DropIndex(
                name: "IX_OrderItem_InventoryId",
                table: "OrderItem");

            migrationBuilder.DropIndex(
                name: "IX_CartItem_InventoryId",
                table: "CartItem");

            migrationBuilder.DropColumn(
                name: "InventoryId",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "InventoryId",
                table: "CartItem");

            migrationBuilder.RenameColumn(
                name: "ServiceId",
                table: "ServiceRegistration",
                newName: "NurseryId");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceRegistration_ServiceId",
                table: "ServiceRegistration",
                newName: "IX_ServiceRegistration_NurseryId");

            migrationBuilder.RenameColumn(
                name: "ServiceId",
                table: "OrderItem",
                newName: "NurseryPlantComboId");

            migrationBuilder.RenameColumn(
                name: "PlantId",
                table: "OrderItem",
                newName: "NurseryMaterialId");

            migrationBuilder.RenameColumn(
                name: "PlantComboId",
                table: "OrderItem",
                newName: "CommonPlantId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_PlantId",
                table: "OrderItem",
                newName: "IX_OrderItem_NurseryMaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_PlantComboId",
                table: "OrderItem",
                newName: "IX_OrderItem_CommonPlantId");

            migrationBuilder.RenameColumn(
                name: "PlantInstanceId",
                table: "CartItem",
                newName: "NurseryPlantComboId");

            migrationBuilder.RenameColumn(
                name: "PlantId",
                table: "CartItem",
                newName: "NurseryMaterialId");

            migrationBuilder.RenameColumn(
                name: "PlantComboId",
                table: "CartItem",
                newName: "CommonPlantId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItem_PlantInstanceId",
                table: "CartItem",
                newName: "IX_CartItem_NurseryPlantComboId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItem_PlantId",
                table: "CartItem",
                newName: "IX_CartItem_NurseryMaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItem_PlantComboId",
                table: "CartItem",
                newName: "IX_CartItem_CommonPlantId");

            migrationBuilder.AddColumn<int>(
                name: "NurseryCareServiceId",
                table: "ServiceRegistration",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NurseryId",
                table: "Order",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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
                        name: "CommonPlant_NurseryId_fkey",
                        column: x => x.NurseryId,
                        principalTable: "Nursery",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "CommonPlant_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
                        principalColumn: "Id");
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Material_pkey", x => x.Id);
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
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
                name: "NurseryPlantCombo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantComboId = table.Column<int>(type: "integer", nullable: false),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
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
                name: "NurseryMaterial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
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
                name: "IX_OrderItem_NurseryPlantComboId",
                table: "OrderItem",
                column: "NurseryPlantComboId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_NurseryId",
                table: "Order",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_CommonPlant_NurseryId",
                table: "CommonPlant",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_CommonPlant_PlantId",
                table: "CommonPlant",
                column: "PlantId");

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
                name: "IX_NurseryCareService_CareServicePackageId",
                table: "NurseryCareService",
                column: "CareServicePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryCareService_NurseryId",
                table: "NurseryCareService",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryMaterial_MaterialId",
                table: "NurseryMaterial",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryMaterial_NurseryId",
                table: "NurseryMaterial",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryPlantCombo_NurseryId",
                table: "NurseryPlantCombo",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_NurseryPlantCombo_PlantComboId",
                table: "NurseryPlantCombo",
                column: "PlantComboId");

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
                name: "Order_NurseryId_fkey",
                table: "Order",
                column: "NurseryId",
                principalTable: "Nursery",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "OrderItem_CommonPlantId_fkey",
                table: "OrderItem",
                column: "CommonPlantId",
                principalTable: "CommonPlant",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "OrderItem_NurseryMaterialId_fkey",
                table: "OrderItem",
                column: "NurseryMaterialId",
                principalTable: "NurseryMaterial",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "OrderItem_NurseryPlantComboId_fkey",
                table: "OrderItem",
                column: "NurseryPlantComboId",
                principalTable: "NurseryPlantCombo",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRegistration_Nursery_NurseryId",
                table: "ServiceRegistration",
                column: "NurseryId",
                principalTable: "Nursery",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "ServiceRegistration_NurseryCareServiceId_fkey",
                table: "ServiceRegistration",
                column: "NurseryCareServiceId",
                principalTable: "NurseryCareService",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "CartItem_CommonPlantId_fkey",
                table: "CartItem");

            migrationBuilder.DropForeignKey(
                name: "CartItem_NurseryMaterialId_fkey",
                table: "CartItem");

            migrationBuilder.DropForeignKey(
                name: "CartItem_NurseryPlantComboId_fkey",
                table: "CartItem");

            migrationBuilder.DropForeignKey(
                name: "Order_NurseryId_fkey",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "OrderItem_CommonPlantId_fkey",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "OrderItem_NurseryMaterialId_fkey",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "OrderItem_NurseryPlantComboId_fkey",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRegistration_Nursery_NurseryId",
                table: "ServiceRegistration");

            migrationBuilder.DropForeignKey(
                name: "ServiceRegistration_NurseryCareServiceId_fkey",
                table: "ServiceRegistration");

            migrationBuilder.DropTable(
                name: "CommonPlant");

            migrationBuilder.DropTable(
                name: "MaterialCategory");

            migrationBuilder.DropTable(
                name: "MaterialImage");

            migrationBuilder.DropTable(
                name: "MaterialTag");

            migrationBuilder.DropTable(
                name: "NurseryCareService");

            migrationBuilder.DropTable(
                name: "NurseryMaterial");

            migrationBuilder.DropTable(
                name: "NurseryPlantCombo");

            migrationBuilder.DropTable(
                name: "Material");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRegistration_NurseryCareServiceId",
                table: "ServiceRegistration");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRegistration_OrderId",
                table: "ServiceRegistration");

            migrationBuilder.DropIndex(
                name: "IX_OrderItem_NurseryPlantComboId",
                table: "OrderItem");

            migrationBuilder.DropIndex(
                name: "IX_Order_NurseryId",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "NurseryCareServiceId",
                table: "ServiceRegistration");

            migrationBuilder.DropColumn(
                name: "NurseryId",
                table: "Order");

            migrationBuilder.RenameColumn(
                name: "NurseryId",
                table: "ServiceRegistration",
                newName: "ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceRegistration_NurseryId",
                table: "ServiceRegistration",
                newName: "IX_ServiceRegistration_ServiceId");

            migrationBuilder.RenameColumn(
                name: "NurseryPlantComboId",
                table: "OrderItem",
                newName: "ServiceId");

            migrationBuilder.RenameColumn(
                name: "NurseryMaterialId",
                table: "OrderItem",
                newName: "PlantId");

            migrationBuilder.RenameColumn(
                name: "CommonPlantId",
                table: "OrderItem",
                newName: "PlantComboId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_NurseryMaterialId",
                table: "OrderItem",
                newName: "IX_OrderItem_PlantId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_CommonPlantId",
                table: "OrderItem",
                newName: "IX_OrderItem_PlantComboId");

            migrationBuilder.RenameColumn(
                name: "NurseryPlantComboId",
                table: "CartItem",
                newName: "PlantInstanceId");

            migrationBuilder.RenameColumn(
                name: "NurseryMaterialId",
                table: "CartItem",
                newName: "PlantId");

            migrationBuilder.RenameColumn(
                name: "CommonPlantId",
                table: "CartItem",
                newName: "PlantComboId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItem_NurseryPlantComboId",
                table: "CartItem",
                newName: "IX_CartItem_PlantInstanceId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItem_NurseryMaterialId",
                table: "CartItem",
                newName: "IX_CartItem_PlantId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItem_CommonPlantId",
                table: "CartItem",
                newName: "IX_CartItem_PlantComboId");

            migrationBuilder.AddColumn<int>(
                name: "InventoryId",
                table: "OrderItem",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InventoryId",
                table: "CartItem",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExpiryMonths = table.Column<int>(type: "integer", nullable: true),
                    InventoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Specifications = table.Column<string>(type: "jsonb", nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Inventory_pkey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantInventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlantInventory_pkey", x => x.Id);
                    table.ForeignKey(
                        name: "PlantInventory_NurseryId_fkey",
                        column: x => x.NurseryId,
                        principalTable: "Nursery",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "PlantInventory_PlantId_fkey",
                        column: x => x.PlantId,
                        principalTable: "Plant",
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ImageUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRegistration_OrderId",
                table: "ServiceRegistration",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_InventoryId",
                table: "OrderItem",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_InventoryId",
                table: "CartItem",
                column: "InventoryId");

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
                name: "IX_PlantInventory_NurseryId",
                table: "PlantInventory",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantInventory_PlantId",
                table: "PlantInventory",
                column: "PlantId");

            migrationBuilder.AddForeignKey(
                name: "CartItem_InventoryId_fkey",
                table: "CartItem",
                column: "InventoryId",
                principalTable: "Inventory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "CartItem_PlantComboId_fkey",
                table: "CartItem",
                column: "PlantComboId",
                principalTable: "PlantCombo",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "CartItem_PlantId_fkey",
                table: "CartItem",
                column: "PlantId",
                principalTable: "Plant",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "CartItem_PlantInstanceId_fkey",
                table: "CartItem",
                column: "PlantInstanceId",
                principalTable: "PlantInstance",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "OrderItem_InventoryId_fkey",
                table: "OrderItem",
                column: "InventoryId",
                principalTable: "Inventory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "OrderItem_PlantComboId_fkey",
                table: "OrderItem",
                column: "PlantComboId",
                principalTable: "PlantCombo",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "OrderItem_PlantId_fkey",
                table: "OrderItem",
                column: "PlantId",
                principalTable: "Plant",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "ServiceRegistration_ServiceId_fkey",
                table: "ServiceRegistration",
                column: "ServiceId",
                principalTable: "CareServicePackage",
                principalColumn: "Id");
        }
    }
}
