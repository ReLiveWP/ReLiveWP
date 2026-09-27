using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ClearingHouse.Migrations
{
    /// <inheritdoc />
    public partial class SyncEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DetachAfterRun",
                table: "ContactSyncSources",
                newName: "SyncEnabled");

            migrationBuilder.RenameIndex(
                name: "IX_ContactSyncSources_DetachAfterRun",
                table: "ContactSyncSources",
                newName: "IX_ContactSyncSources_SyncEnabled");

            migrationBuilder.Sql(@"UPDATE ""ContactSyncSources"" SET ""SyncEnabled"" = NOT ""SyncEnabled"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE ""ContactSyncSources"" SET ""SyncEnabled"" = NOT ""SyncEnabled"";");

            migrationBuilder.RenameColumn(
                name: "SyncEnabled",
                table: "ContactSyncSources",
                newName: "DetachAfterRun");

            migrationBuilder.RenameIndex(
                name: "IX_ContactSyncSources_SyncEnabled",
                table: "ContactSyncSources",
                newName: "IX_ContactSyncSources_DetachAfterRun");
        }
    }
}
