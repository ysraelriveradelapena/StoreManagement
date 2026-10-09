using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryHistoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_StoreId_CreatedAtUtc",
                table: "InventoryTransactions",
                columns: new[] { "StoreId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_StoreId_CreatedAtUtc",
                table: "InventoryTransactions");
        }
    }
}
