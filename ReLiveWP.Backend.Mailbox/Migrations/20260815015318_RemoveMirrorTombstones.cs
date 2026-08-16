using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMirrorTombstones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MirrorTombstones");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MirrorTombstones",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OriginCollectionId = table.Column<string>(type: "text", nullable: false),
                    OriginExternalId = table.Column<string>(type: "text", nullable: false),
                    OriginServiceId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MirrorTombstones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MirrorTombstones_UserId_OriginServiceId_OriginCollectionId_~",
                table: "MirrorTombstones",
                columns: new[] { "UserId", "OriginServiceId", "OriginCollectionId", "OriginExternalId" },
                unique: true);
        }
    }
}
