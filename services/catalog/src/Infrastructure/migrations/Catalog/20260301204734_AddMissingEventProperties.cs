using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingEventProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seats_EventId",
                schema: "bc_catalog",
                table: "Seats");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "bc_catalog",
                table: "Events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "MaxCapacity",
                schema: "bc_catalog",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "bc_catalog",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "active");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "bc_catalog",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Venue",
                schema: "bc_catalog",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "bc_catalog",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                schema: "bc_catalog",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "bc_catalog",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "bc_catalog",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Venue",
                schema: "bc_catalog",
                table: "Events");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_EventId",
                schema: "bc_catalog",
                table: "Seats",
                column: "EventId");
        }
    }
}
