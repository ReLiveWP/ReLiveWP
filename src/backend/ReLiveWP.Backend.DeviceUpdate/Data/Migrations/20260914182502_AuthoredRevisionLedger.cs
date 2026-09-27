using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.DeviceUpdate.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuthoredRevisionLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthoredRevisions",
                columns: table => new
                {
                    UpdateId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    RevisionId = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthoredRevisions", x => new { x.UpdateId, x.RevisionNumber });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthoredRevisions_RevisionId",
                table: "AuthoredRevisions",
                column: "RevisionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthoredRevisions");
        }
    }
}
