using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Identity.Migrations
{
    /// <inheritdoc />
    public partial class OAuthPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectedServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Service = table.Column<string>(type: "TEXT", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Flags = table.Column<uint>(type: "INTEGER", nullable: false),
                    EnabledCapabilities = table.Column<uint>(type: "INTEGER", nullable: false),
                    DPoPKeyId = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorizationEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    TokenEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceProfile_Username = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_EmailAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_AvatarUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LiveUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectedServices_AspNetUsers_LiveUserId",
                        column: x => x.LiveUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedServices_LiveUserId",
                table: "ConnectedServices",
                column: "LiveUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectedServices");
        }
    }
}
