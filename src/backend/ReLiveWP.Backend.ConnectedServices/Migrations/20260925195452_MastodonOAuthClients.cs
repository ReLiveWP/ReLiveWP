using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ConnectedServices.Migrations
{
    /// <inheritdoc />
    public partial class MastodonOAuthClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthClients",
                columns: table => new
                {
                    Authority = table.Column<string>(type: "text", nullable: false),
                    Service = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    EncryptedSecret = table.Column<string>(type: "text", nullable: false),
                    RedirectUri = table.Column<string>(type: "text", nullable: false),
                    RequestedScopes = table.Column<string>(type: "text", nullable: false),
                    RegisteredScopes = table.Column<string>(type: "text", nullable: false),
                    AuthorizationEndpoint = table.Column<string>(type: "text", nullable: false),
                    TokenEndpoint = table.Column<string>(type: "text", nullable: false),
                    RevocationEndpoint = table.Column<string>(type: "text", nullable: true),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthClients", x => x.Authority);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthClients");
        }
    }
}
