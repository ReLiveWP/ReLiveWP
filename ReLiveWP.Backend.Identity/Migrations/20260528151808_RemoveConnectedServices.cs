using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Identity.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConnectedServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectedServices");

            migrationBuilder.DropTable(
                name: "PendingOAuths");

            migrationBuilder.DropTable(
                name: "DPoPKeys");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DPoPKeys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DPoPKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingOAuths",
                columns: table => new
                {
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuthorizationEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    CodeVerifier = table.Column<string>(type: "TEXT", nullable: true),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Service = table.Column<string>(type: "TEXT", nullable: false),
                    TokenEndpoint = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingOAuths", x => x.State);
                    table.ForeignKey(
                        name: "FK_PendingOAuths_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectedServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DPoPKeyId = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorizationEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    EnabledCapabilities = table.Column<uint>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Flags = table.Column<uint>(type: "INTEGER", nullable: false),
                    Issuer = table.Column<string>(type: "TEXT", nullable: true),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    Service = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    TokenEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_AvatarUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_EmailAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceProfile_Username = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectedServices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConnectedServices_DPoPKeys_DPoPKeyId",
                        column: x => x.DPoPKeyId,
                        principalTable: "DPoPKeys",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedServices_DPoPKeyId",
                table: "ConnectedServices",
                column: "DPoPKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedServices_UserId",
                table: "ConnectedServices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingOAuths_State",
                table: "PendingOAuths",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_PendingOAuths_UserId",
                table: "PendingOAuths",
                column: "UserId");
        }
    }
}
