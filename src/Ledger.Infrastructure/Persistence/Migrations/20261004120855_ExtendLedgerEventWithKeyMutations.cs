using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendLedgerEventWithKeyMutations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<byte[]>>(
                name: "keys_added",
                table: "ledger_event",
                type: "bytea[]",
                nullable: false);

            migrationBuilder.AddColumn<List<byte[]>>(
                name: "keys_removed",
                table: "ledger_event",
                type: "bytea[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "keys_added",
                table: "ledger_event");

            migrationBuilder.DropColumn(
                name: "keys_removed",
                table: "ledger_event");
        }
    }
}
