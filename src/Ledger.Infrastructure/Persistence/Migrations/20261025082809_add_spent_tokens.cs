using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_spent_tokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spent_token",
                columns: table => new
                {
                    nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    token_key_id = table.Column<byte[]>(type: "bytea", nullable: false),
                    spent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spent_token", x => x.nonce);
                });

            migrationBuilder.CreateIndex(
                name: "IX_spent_token_token_key_id",
                table: "spent_token",
                column: "token_key_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "spent_token");
        }
    }
}
