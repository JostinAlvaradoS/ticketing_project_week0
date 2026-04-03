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
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_waitlist_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_waitlist_active",
                schema: "bc_waitlist",
                table: "waitlist_entries",
                columns: new[] { "Email", "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "idx_waitlist_expiry",
                schema: "bc_waitlist",
                table: "waitlist_entries",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "idx_waitlist_fifo",
                schema: "bc_waitlist",
                table: "waitlist_entries",
                columns: new[] { "EventId", "Status", "CreatedAt" });
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
