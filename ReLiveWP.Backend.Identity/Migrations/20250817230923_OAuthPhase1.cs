using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Identity.Migrations
{
    /// <inheritdoc />
    public partial class OAuthPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingOAuths",
                columns: table => new
                {
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Service = table.Column<string>(type: "TEXT", nullable: false),
                    CodeVerifier = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorizationEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    TokenEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    LiveUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingOAuths", x => x.State);
                    table.ForeignKey(
                        name: "FK_PendingOAuths_AspNetUsers_LiveUserId",
                        column: x => x.LiveUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingOAuths_LiveUserId",
                table: "PendingOAuths",
                column: "LiveUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingOAuths_State",
                table: "PendingOAuths",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingOAuths");
        }
    }
}
