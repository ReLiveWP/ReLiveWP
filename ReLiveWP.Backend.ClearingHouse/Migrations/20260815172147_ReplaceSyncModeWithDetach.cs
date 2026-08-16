using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ClearingHouse.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSyncModeWithDetach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DetachAfterRun",
                table: "ContactSyncSources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Mode 0 was ImportOnce and 1 was Mirror. Without this every import-once source defaults
            // into being a mirror and starts getting polled, which is the opposite of what it said.
            migrationBuilder.Sql(
                """UPDATE "ContactSyncSources" SET "DetachAfterRun" = ("Mode" = 0);""");

            migrationBuilder.DropIndex(
                name: "IX_ContactSyncSources_Mode",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "ContactSyncSources");

            migrationBuilder.CreateIndex(
                name: "IX_ContactSyncSources_DetachAfterRun",
                table: "ContactSyncSources",
                column: "DetachAfterRun");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContactSyncSources_DetachAfterRun",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "DetachAfterRun",
                table: "ContactSyncSources");

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ContactSyncSources_Mode",
                table: "ContactSyncSources",
                column: "Mode");
        }
    }
}
