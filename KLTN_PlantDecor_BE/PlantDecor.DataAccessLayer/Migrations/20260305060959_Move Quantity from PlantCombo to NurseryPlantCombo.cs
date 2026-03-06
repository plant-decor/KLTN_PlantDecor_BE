using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantDecor.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class MoveQuantityfromPlantCombotoNurseryPlantCombo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "PlantCombo");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "NurseryPlantCombo",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "NurseryPlantCombo");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "PlantCombo",
                type: "integer",
                nullable: true);
        }
    }
}
