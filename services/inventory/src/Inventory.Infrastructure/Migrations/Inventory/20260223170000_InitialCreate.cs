using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations.Inventory
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bc_inventory");

            migrationBuilder.CreateTable(
                name: "Seats",
                schema: "bc_inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false),
                    Row = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Reserved = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Seats",
                schema: "bc_inventory");
        }
    }
}
