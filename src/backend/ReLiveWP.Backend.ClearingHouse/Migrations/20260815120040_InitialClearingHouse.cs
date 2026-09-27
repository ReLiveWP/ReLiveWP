using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ClearingHouse.Migrations
{
    /// <inheritdoc />
    public partial class InitialClearingHouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactSyncSources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ConnectionId = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    SourceId = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    DeltaToken = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailureAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailure = table.Column<string>(type: "text", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactSyncSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MirrorTombstones",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    CollectionId = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MirrorTombstones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactSyncSources_Mode",
                table: "ContactSyncSources",
                column: "Mode");

            migrationBuilder.CreateIndex(
                name: "IX_ContactSyncSources_UserId_ConnectionId_SourceId",
                table: "ContactSyncSources",
                columns: new[] { "UserId", "ConnectionId", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MirrorTombstones_UserId_ServiceId_CollectionId_ExternalId",
                table: "MirrorTombstones",
                columns: new[] { "UserId", "ServiceId", "CollectionId", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactSyncSources");

            migrationBuilder.DropTable(
                name: "MirrorTombstones");
        }
    }
}
