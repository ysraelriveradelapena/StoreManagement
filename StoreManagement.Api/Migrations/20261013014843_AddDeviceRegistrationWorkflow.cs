using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceRegistrationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Devices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Devices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CredentialCreatedAtUtc",
                table: "Devices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CredentialHash",
                table: "Devices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceRegistrationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceRegistrationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceRegistrationRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceRegistrationRequests_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrationRequests_RequestedByUserId",
                table: "DeviceRegistrationRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrationRequests_Status_RequestedAtUtc",
                table: "DeviceRegistrationRequests",
                columns: new[] { "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrationRequests_StoreId",
                table: "DeviceRegistrationRequests",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "CredentialCreatedAtUtc",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "CredentialHash",
                table: "Devices");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Devices",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
