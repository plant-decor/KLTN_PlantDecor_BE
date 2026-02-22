using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlantDecor.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class ModifyDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "ChatParticipant_ChatSessionId_fkey",
                table: "ChatParticipant");

            migrationBuilder.DropForeignKey(
                name: "ChatParticipant_UserId_fkey",
                table: "ChatParticipant");

            migrationBuilder.DropForeignKey(
                name: "Order_UserId_fkey",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "PlantImage_PlantId_fkey",
                table: "PlantImage");

            migrationBuilder.DropForeignKey(
                name: "Wishlist_PlantId_fkey",
                table: "Wishlist");

            migrationBuilder.DropForeignKey(
                name: "Wishlist_UserId_fkey",
                table: "Wishlist");

            migrationBuilder.DropPrimaryKey(
                name: "Wishlist_pkey",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_Wishlist_UserId",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_UserPlant_PlantInstanceId",
                table: "UserPlant");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRating_ServiceRegistrationId",
                table: "ServiceRating");

            migrationBuilder.DropIndex(
                name: "Plant_PlantCode_key",
                table: "Plant");

            migrationBuilder.DropIndex(
                name: "IX_Nursery_ManagerId",
                table: "Nursery");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_OrderId",
                table: "Invoice");

            migrationBuilder.DropPrimaryKey(
                name: "ChatParticipant_pkey",
                table: "ChatParticipant");

            migrationBuilder.DropIndex(
                name: "IX_ChatParticipant_ChatSessionId",
                table: "ChatParticipant");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Wishlist");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Wishlist");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Wishlist");

            migrationBuilder.DropColumn(
                name: "PlantCode",
                table: "Plant");

            migrationBuilder.DropColumn(
                name: "AllowedToAll",
                table: "LayoutDesign");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ChatParticipant");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "CartItem");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Cart");

            migrationBuilder.RenameColumn(
                name: "SalePrice",
                table: "PlantCombo",
                newName: "ComboPrice");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Wishlist",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "Wishlist",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchaseHistoryScore",
                table: "UserPreference",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ProfileMatchScore",
                table: "UserPreference",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PreferenceScore",
                table: "UserPreference",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BehaviorScore",
                table: "UserPreference",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentTrunkDiameter",
                table: "UserPlant",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentHeight",
                table: "UserPlant",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "ServiceRegistration",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "ServiceRegistration",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ServiceRating",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TrunkDiameter",
                table: "PlantInstance",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Height",
                table: "PlantInstance",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentNurseryId",
                table: "PlantInstance",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "PlantInstance",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "PlantImage",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlantInstanceId",
                table: "PlantImage",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ThemeDescription",
                table: "PlantCombo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FengShuiPurpose",
                table: "PlantCombo",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Order",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShipperNote",
                table: "Order",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShipperId",
                table: "Order",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Nursery",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Nursery",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Area",
                table: "Nursery",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ChatParticipant",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ChatSessionId",
                table: "ChatParticipant",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Wishlist",
                table: "Wishlist",
                columns: new[] { "UserId", "PlantId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatParticipant",
                table: "ChatParticipant",
                columns: new[] { "ChatSessionId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPlant_PlantInstanceId",
                table: "UserPlant",
                column: "PlantInstanceId",
                unique: true);

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
                name: "IX_PlantInstance_CurrentNurseryId",
                table: "PlantInstance",
                column: "CurrentNurseryId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantImage_PlantInstanceId",
                table: "PlantImage",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Order_ShipperId",
                table: "Order",
                column: "ShipperId");

            migrationBuilder.CreateIndex(
                name: "IX_Nursery_ManagerId",
                table: "Nursery",
                column: "ManagerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_OrderId",
                table: "Invoice",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatParticipant_ChatSession_ChatSessionId",
                table: "ChatParticipant",
                column: "ChatSessionId",
                principalTable: "ChatSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatParticipant_User_UserId",
                table: "ChatParticipant",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Order_User_UserId",
                table: "Order",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "Order_ShipperId_fkey",
                table: "Order",
                column: "ShipperId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "PlantImage_PlantId_fkey",
                table: "PlantImage",
                column: "PlantId",
                principalTable: "Plant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "PlantImage_PlantInstance_fkey",
                table: "PlantImage",
                column: "PlantInstanceId",
                principalTable: "PlantInstance",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "PlantInstance_NurseryId_fkey",
                table: "PlantInstance",
                column: "CurrentNurseryId",
                principalTable: "Nursery",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "ServiceRating_UserId_fkey",
                table: "ServiceRating",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "Wishlist_PlantId_fkey",
                table: "Wishlist",
                column: "PlantId",
                principalTable: "Plant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "Wishlist_UserId_fkey",
                table: "Wishlist",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatParticipant_ChatSession_ChatSessionId",
                table: "ChatParticipant");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatParticipant_User_UserId",
                table: "ChatParticipant");

            migrationBuilder.DropForeignKey(
                name: "FK_Order_User_UserId",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "Order_ShipperId_fkey",
                table: "Order");

            migrationBuilder.DropForeignKey(
                name: "PlantImage_PlantId_fkey",
                table: "PlantImage");

            migrationBuilder.DropForeignKey(
                name: "PlantImage_PlantInstance_fkey",
                table: "PlantImage");

            migrationBuilder.DropForeignKey(
                name: "PlantInstance_NurseryId_fkey",
                table: "PlantInstance");

            migrationBuilder.DropForeignKey(
                name: "ServiceRating_UserId_fkey",
                table: "ServiceRating");

            migrationBuilder.DropForeignKey(
                name: "Wishlist_PlantId_fkey",
                table: "Wishlist");

            migrationBuilder.DropForeignKey(
                name: "Wishlist_UserId_fkey",
                table: "Wishlist");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Wishlist",
                table: "Wishlist");

            migrationBuilder.DropIndex(
                name: "IX_UserPlant_PlantInstanceId",
                table: "UserPlant");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRating_ServiceRegistrationId",
                table: "ServiceRating");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRating_UserId",
                table: "ServiceRating");

            migrationBuilder.DropIndex(
                name: "IX_PlantInstance_CurrentNurseryId",
                table: "PlantInstance");

            migrationBuilder.DropIndex(
                name: "IX_PlantImage_PlantInstanceId",
                table: "PlantImage");

            migrationBuilder.DropIndex(
                name: "IX_Order_ShipperId",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "IX_Nursery_ManagerId",
                table: "Nursery");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_OrderId",
                table: "Invoice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatParticipant",
                table: "ChatParticipant");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "ServiceRegistration");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "ServiceRegistration");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ServiceRating");

            migrationBuilder.DropColumn(
                name: "CurrentNurseryId",
                table: "PlantInstance");

            migrationBuilder.DropColumn(
                name: "SKU",
                table: "PlantInstance");

            migrationBuilder.DropColumn(
                name: "PlantInstanceId",
                table: "PlantImage");

            migrationBuilder.DropColumn(
                name: "ShipperId",
                table: "Order");

            migrationBuilder.RenameColumn(
                name: "ComboPrice",
                table: "PlantCombo",
                newName: "SalePrice");

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "Wishlist",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Wishlist",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Wishlist",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Wishlist",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Wishlist",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchaseHistoryScore",
                table: "UserPreference",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ProfileMatchScore",
                table: "UserPreference",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PreferenceScore",
                table: "UserPreference",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BehaviorScore",
                table: "UserPreference",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentTrunkDiameter",
                table: "UserPlant",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentHeight",
                table: "UserPlant",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TrunkDiameter",
                table: "PlantInstance",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Height",
                table: "PlantInstance",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "PlantImage",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "ThemeDescription",
                table: "PlantCombo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FengShuiPurpose",
                table: "PlantCombo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlantCode",
                table: "Plant",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Order",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "ShipperNote",
                table: "Order",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Nursery",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,7)",
                oldPrecision: 10,
                oldScale: 7,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Nursery",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,7)",
                oldPrecision: 10,
                oldScale: 7,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Area",
                table: "Nursery",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedToAll",
                table: "LayoutDesign",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ChatParticipant",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ChatSessionId",
                table: "ChatParticipant",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ChatParticipant",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "CartItem",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Cart",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "Wishlist_pkey",
                table: "Wishlist",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "ChatParticipant_pkey",
                table: "ChatParticipant",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlist_UserId",
                table: "Wishlist",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlant_PlantInstanceId",
                table: "UserPlant",
                column: "PlantInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRating_ServiceRegistrationId",
                table: "ServiceRating",
                column: "ServiceRegistrationId");

            migrationBuilder.CreateIndex(
                name: "Plant_PlantCode_key",
                table: "Plant",
                column: "PlantCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nursery_ManagerId",
                table: "Nursery",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_OrderId",
                table: "Invoice",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipant_ChatSessionId",
                table: "ChatParticipant",
                column: "ChatSessionId");

            migrationBuilder.AddForeignKey(
                name: "ChatParticipant_ChatSessionId_fkey",
                table: "ChatParticipant",
                column: "ChatSessionId",
                principalTable: "ChatSession",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "ChatParticipant_UserId_fkey",
                table: "ChatParticipant",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "Order_UserId_fkey",
                table: "Order",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "PlantImage_PlantId_fkey",
                table: "PlantImage",
                column: "PlantId",
                principalTable: "Plant",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "Wishlist_PlantId_fkey",
                table: "Wishlist",
                column: "PlantId",
                principalTable: "Plant",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "Wishlist_UserId_fkey",
                table: "Wishlist",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");
        }
    }
}
