using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waitlist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bc_waitlist");

            migrationBuilder.CreateTable(
                name: "waitlist_entries",
                schema: "bc_waitlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_waitlist_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_waitlist_expiry",
                schema: "bc_waitlist",
                table: "waitlist_entries",
                column: "ExpiresAt",
                filter: "\"Status\" = 'assigned'");

            migrationBuilder.CreateIndex(
                name: "idx_waitlist_fifo",
                schema: "bc_waitlist",
                table: "waitlist_entries",
                columns: new[] { "EventId", "Status", "RegisteredAt" });

            migrationBuilder.CreateIndex(
                name: "idx_waitlist_order",
                schema: "bc_waitlist",
                table: "waitlist_entries",
                column: "OrderId",
                filter: "\"OrderId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "waitlist_entries",
                schema: "bc_waitlist");
        }
    }
}
