using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.SkyDrive.Migrations
{
    /// <inheritdoc />
    public partial class AlbumCovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlbumCovers",
                columns: table => new
                {
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlbumRef = table.Column<string>(type: "text", nullable: false),
                    ResourceRef = table.Column<string>(type: "text", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumCovers", x => new { x.OwnerId, x.AlbumRef });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlbumCovers");
        }
    }
}
