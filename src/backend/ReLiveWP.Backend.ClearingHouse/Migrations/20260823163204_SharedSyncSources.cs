using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ClearingHouse.Migrations
{
    /// <inheritdoc />
    public partial class SharedSyncSources : Migration
    {
        // hand-written. the scaffolded version dropped and recreated the table, which would have
        // thrown away every delta token and left SyncEnabled false, silently turning contact sync
        // off for everyone.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ContactSyncSources",
                newName: "SyncSources");

            migrationBuilder.Sql(
                """ALTER TABLE "SyncSources" RENAME CONSTRAINT "PK_ContactSyncSources" TO "PK_SyncSources";""");

            migrationBuilder.RenameIndex(
                name: "IX_ContactSyncSources_SyncEnabled",
                table: "SyncSources",
                newName: "IX_SyncSources_SyncEnabled");

            migrationBuilder.DropIndex(
                name: "IX_ContactSyncSources_UserId_ConnectionId_SourceId",
                table: "SyncSources");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "SyncSources",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""UPDATE "SyncSources" SET "Kind" = 'Contacts' WHERE "Kind" IS NULL;""");

            // the old* arguments are what make this emit SET NOT NULL; without them EF sees no change
            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "SyncSources",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FolderId",
                table: "SyncSources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemoteDisplayName",
                table: "SyncSources",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncSources_UserId_ConnectionId_Kind_SourceId",
                table: "SyncSources",
                columns: new[] { "UserId", "ConnectionId", "Kind", "SourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncSources_UserId_ConnectionId_Kind_SourceId",
                table: "SyncSources");

            // calendar rows have no home in the contacts-only shape
            migrationBuilder.Sql("""DELETE FROM "SyncSources" WHERE "Kind" <> 'Contacts';""");

            migrationBuilder.DropColumn(name: "RemoteDisplayName", table: "SyncSources");
            migrationBuilder.DropColumn(name: "FolderId", table: "SyncSources");
            migrationBuilder.DropColumn(name: "Kind", table: "SyncSources");

            migrationBuilder.CreateIndex(
                name: "IX_ContactSyncSources_UserId_ConnectionId_SourceId",
                table: "SyncSources",
                columns: new[] { "UserId", "ConnectionId", "SourceId" },
                unique: true);

            migrationBuilder.RenameIndex(
                name: "IX_SyncSources_SyncEnabled",
                table: "SyncSources",
                newName: "IX_ContactSyncSources_SyncEnabled");

            migrationBuilder.Sql(
                """ALTER TABLE "SyncSources" RENAME CONSTRAINT "PK_SyncSources" TO "PK_ContactSyncSources";""");

            migrationBuilder.RenameTable(
                name: "SyncSources",
                newName: "ContactSyncSources");
        }
    }
}
