using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ClearingHouse.Migrations
{
    /// <inheritdoc />
    public partial class DropWriteBackState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MirrorTombstones");

            migrationBuilder.DropColumn(
                name: "LastRunConflicts",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "LastRunRefused",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "LastRunWritten",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "LastWriteBackAt",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "WriteBackRequestedAt",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "Writes",
                table: "ContactSyncSources");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastRunConflicts",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LastRunRefused",
                table: "ContactSyncSources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LastRunWritten",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWriteBackAt",
                table: "ContactSyncSources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WriteBackRequestedAt",
                table: "ContactSyncSources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Writes",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MirrorTombstones",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CollectionId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MirrorTombstones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MirrorTombstones_UserId_ServiceId_CollectionId_ExternalId",
                table: "MirrorTombstones",
                columns: new[] { "UserId", "ServiceId", "CollectionId", "ExternalId" },
                unique: true);
        }
    }
}
