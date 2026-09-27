using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ConnectedServices.Migrations
{
    /// <inheritdoc />
    public partial class DropPendingOAuths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingOAuths");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingOAuths",
                columns: table => new
                {
                    State = table.Column<string>(type: "text", nullable: false),
                    AuthorizationEndpoint = table.Column<string>(type: "text", nullable: true),
                    CodeVerifier = table.Column<string>(type: "text", nullable: true),
                    Endpoint = table.Column<string>(type: "text", nullable: true),
                    ExistingConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Service = table.Column<string>(type: "text", nullable: false),
                    TokenEndpoint = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingOAuths", x => x.State);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingOAuths_State",
                table: "PendingOAuths",
                column: "State");
        }
    }
}
