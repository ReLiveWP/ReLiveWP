using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ConnectedServices.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                    Service = table.Column<string>(type: "TEXT", nullable: false),
                    CodeVerifier = table.Column<string>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorizationEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    TokenEndpoint = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingOAuths", x => x.State);
                });

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
                    RowVersion = table.Column<uint>(type: "INTEGER", nullable: false),
                    DPoPKeyId = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorizationEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    TokenEndpoint = table.Column<string>(type: "TEXT", nullable: true),
                    Issuer = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceProfile_Username = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_EmailAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ServiceProfile_AvatarUrl = table.Column<string>(type: "TEXT", nullable: true)
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
