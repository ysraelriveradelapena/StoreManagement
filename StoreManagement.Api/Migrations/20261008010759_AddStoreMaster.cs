using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Stores_StoreId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_Stores_Code",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_StoreId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "ProductVariants");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentStore",
                table: "Stores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Stores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreCode",
                table: "Stores",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoreName",
                table: "Stores",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_StoreCode",
                table: "Stores",
                column: "StoreCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StoreId",
                table: "AspNetUsers",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Stores_StoreId",
                table: "AspNetUsers",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Stores_StoreId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Stores_StoreCode",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_StoreId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DepartmentStore",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "StoreCode",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "StoreName",
                table: "Stores");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Stores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Stores",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Stores",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "ProductVariants",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stores_Code",
                table: "Stores",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_StoreId",
                table: "ProductVariants",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Stores_StoreId",
                table: "ProductVariants",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");
        }
    }
}
