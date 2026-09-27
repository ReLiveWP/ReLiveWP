using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReLiveWP.Services.Push.Migrations
{
    /// <inheritdoc />
    public partial class DropTransientPushTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<long>(type: "bigint", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    LeaseExpiresAt = table.Column<long>(type: "bigint", nullable: true),
                    LeaseOwner = table.Column<string>(type: "text", nullable: true),
                    NotificationClass = table.Column<long>(type: "bigint", nullable: false),
                    Payload = table.Column<byte[]>(type: "bytea", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Token = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    LastSeenAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Token);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeviceId_Status",
                table: "Notifications",
                columns: new[] { "DeviceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ExpiresAt",
                table: "Notifications",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Status_LeaseExpiresAt",
                table: "Notifications",
                columns: new[] { "Status", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_DeviceId",
                table: "Sessions",
                column: "DeviceId");
        }
    }
}
