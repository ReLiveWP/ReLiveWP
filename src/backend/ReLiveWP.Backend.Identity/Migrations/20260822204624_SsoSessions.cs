using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Identity.Migrations
{
    /// <inheritdoc />
    public partial class SsoSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SsoSessionId",
                table: "LiveRefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LiveSsoSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AbsoluteExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedIp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveSsoSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiveSsoSessions_TokenHash",
                table: "LiveSsoSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveSsoSessions_UserId",
                table: "LiveSsoSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveSsoSessions");

            migrationBuilder.DropColumn(
                name: "SsoSessionId",
                table: "LiveRefreshTokens");
        }
    }
}
