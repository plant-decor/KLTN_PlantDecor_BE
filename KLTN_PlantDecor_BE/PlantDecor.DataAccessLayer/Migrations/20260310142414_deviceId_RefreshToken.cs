using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantDecor.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class deviceId_RefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModerationStatus",
                table: "LayoutDesign",
                newName: "Status");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRevoked",
                table: "RefreshToken",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "RefreshToken",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "RefreshToken");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "LayoutDesign",
                newName: "ModerationStatus");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRevoked",
                table: "RefreshToken",
                type: "boolean",
                nullable: true,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
