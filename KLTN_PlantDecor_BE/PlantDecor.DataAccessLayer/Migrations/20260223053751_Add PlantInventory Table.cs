using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlantDecor.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantInventoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantInventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    NurseryId = table.Column<int>(type: "integer", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_PlantInventory_NurseryId",
                table: "PlantInventory",
                column: "NurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantInventory_PlantId",
                table: "PlantInventory",
                column: "PlantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantInventory");
        }
    }
}
