using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ConnectedServices.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DPoPKeys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DPoPKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingOAuths",
                columns: table => new
                {
                    State = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Service = table.Column<string>(type: "text", nullable: false),
                    CodeVerifier = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: true),
                    AuthorizationEndpoint = table.Column<string>(type: "text", nullable: true),
                    TokenEndpoint = table.Column<string>(type: "text", nullable: true),
                    ExistingConnectionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingOAuths", x => x.State);
                });

            migrationBuilder.CreateTable(
                name: "ConnectedServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Service = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Flags = table.Column<long>(type: "bigint", nullable: false),
                    AvailableCapabilities = table.Column<long>(type: "bigint", nullable: false),
                    EnabledCapabilities = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    DPoPKeyId = table.Column<string>(type: "text", nullable: true),
                    ServiceUrl = table.Column<string>(type: "text", nullable: true),
                    AuthorizationEndpoint = table.Column<string>(type: "text", nullable: true),
                    TokenEndpoint = table.Column<string>(type: "text", nullable: true),
                    Issuer = table.Column<string>(type: "text", nullable: true),
                    ServiceProfile_UserId = table.Column<string>(type: "text", nullable: false),
                    ServiceProfile_Username = table.Column<string>(type: "text", nullable: true),
                    ServiceProfile_DisplayName = table.Column<string>(type: "text", nullable: true),
                    ServiceProfile_EmailAddress = table.Column<string>(type: "text", nullable: true),
                    ServiceProfile_AvatarUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedServices", x => x.Id);
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
                name: "IX_PendingOAuths_State",
                table: "PendingOAuths",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectedServices");

            migrationBuilder.DropTable(
                name: "PendingOAuths");

            migrationBuilder.DropTable(
                name: "DPoPKeys");
        }
    }
}
