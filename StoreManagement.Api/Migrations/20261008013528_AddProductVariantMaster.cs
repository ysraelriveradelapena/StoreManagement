using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_Code",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IsNonExchangeable",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "PriceType",
                table: "ProductVariants");

            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "ProductVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "ProductVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ProductVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "ProductVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VariantName",
                table: "ProductVariants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_SKU",
                table: "ProductVariants",
                column: "SKU",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_SKU",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "SKU",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantName",
                table: "ProductVariants");

            migrationBuilder.AlterColumn<string>(
                name: "Size",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Barcode",
                table: "ProductVariants",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ProductVariants",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsNonExchangeable",
                table: "ProductVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PriceType",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Code",
                table: "ProductVariants",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_Products_ProductId",
                table: "ProductVariants",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
