using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantDecor.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTagCategoryPlantInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "PlantInstance");

            migrationBuilder.AddColumn<int>(
                name: "TagType",
                table: "Tag",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PlantInstance",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CategoryType",
                table: "Category",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TagType",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "CategoryType",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PlantInstance");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "PlantInstance",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
