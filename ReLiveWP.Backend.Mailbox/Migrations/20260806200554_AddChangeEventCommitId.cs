using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class AddChangeEventCommitId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CommitId",
                table: "ItemEvents",
                type: "bigint",
                nullable: false,
                defaultValueSql: "(pg_current_xact_id()::text::bigint + 1000000000)");

            migrationBuilder.AddColumn<long>(
                name: "CommitId",
                table: "FolderEvents",
                type: "bigint",
                nullable: false,
                defaultValueSql: "(pg_current_xact_id()::text::bigint + 1000000000)");
                
            migrationBuilder.Sql(@"UPDATE ""ItemEvents"" SET ""CommitId"" = ""Id"";");
            migrationBuilder.Sql(@"UPDATE ""FolderEvents"" SET ""CommitId"" = ""Id"";");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEvents_UserId_CollectionId_CommitId_Id",
                table: "ItemEvents",
                columns: new[] { "UserId", "CollectionId", "CommitId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_FolderEvents_UserId_CommitId_Id",
                table: "FolderEvents",
                columns: new[] { "UserId", "CommitId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemEvents_UserId_CollectionId_CommitId_Id",
                table: "ItemEvents");

            migrationBuilder.DropIndex(
                name: "IX_FolderEvents_UserId_CommitId_Id",
                table: "FolderEvents");

            migrationBuilder.DropColumn(
                name: "CommitId",
                table: "ItemEvents");

            migrationBuilder.DropColumn(
                name: "CommitId",
                table: "FolderEvents");
        }
    }
}
