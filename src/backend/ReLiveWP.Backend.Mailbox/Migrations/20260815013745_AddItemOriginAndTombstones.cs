using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class AddItemOriginAndTombstones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginCollectionId",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginEtag",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginExternalId",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginPayload",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginServiceId",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OriginSyncedAt",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MirrorTombstones",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    OriginServiceId = table.Column<string>(type: "text", nullable: false),
                    OriginCollectionId = table.Column<string>(type: "text", nullable: false),
                    OriginExternalId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MirrorTombstones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_OriginServiceId_OriginCollectionId_OriginExter~",
                table: "Items",
                columns: new[] { "UserId", "OriginServiceId", "OriginCollectionId", "OriginExternalId" },
                unique: true,
                filter: "\"OriginServiceId\" IS NOT NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MirrorTombstones_UserId_OriginServiceId_OriginCollectionId_~",
                table: "MirrorTombstones",
                columns: new[] { "UserId", "OriginServiceId", "OriginCollectionId", "OriginExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MirrorTombstones");

            migrationBuilder.DropIndex(
                name: "IX_Items_UserId_OriginServiceId_OriginCollectionId_OriginExter~",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "OriginCollectionId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "OriginEtag",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "OriginExternalId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "OriginPayload",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "OriginServiceId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "OriginSyncedAt",
                table: "Items");
        }
    }
}
