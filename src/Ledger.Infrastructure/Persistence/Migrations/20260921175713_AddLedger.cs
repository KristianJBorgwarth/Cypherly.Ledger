using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ledger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ledger_stream",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    head_version = table.Column<int>(type: "integer", nullable: false),
                    head_hash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    archived = table.Column<bool>(type: "boolean", nullable: false),
                    updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_stream", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stream_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<byte[]>(type: "bytea", nullable: false),
                    previous_hash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    write_key_public = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    signature = table.Column<byte[]>(type: "bytea", maxLength: 64, nullable: false),
                    updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_ledger_event_ledger_stream_stream_id",
                        column: x => x.stream_id,
                        principalTable: "ledger_stream",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ledger_write_key",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stream_id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_key = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_write_key", x => x.id);
                    table.ForeignKey(
                        name: "FK_ledger_write_key_ledger_stream_stream_id",
                        column: x => x.stream_id,
                        principalTable: "ledger_stream",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ledger_event_stream_id_version",
                table: "ledger_event",
                columns: new[] { "stream_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ledger_write_key_stream_id_public_key",
                table: "ledger_write_key",
                columns: new[] { "stream_id", "public_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ledger_event");

            migrationBuilder.DropTable(
                name: "ledger_write_key");

            migrationBuilder.DropTable(
                name: "ledger_stream");
        }
    }
}
