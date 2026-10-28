using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_invite_keys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "role",
                table: "ledger_write_key",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<byte[]>>(
                name: "invite_keys_added",
                table: "ledger_event",
                type: "bytea[]",
                nullable: false,
                defaultValueSql: "'{}'::bytea[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                table: "ledger_write_key");

            migrationBuilder.DropColumn(
                name: "invite_keys_added",
                table: "ledger_event");
        }
    }
}
