using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.DeviceRegistration.Migrations
{
    /// <inheritdoc />
    public partial class ActivationCodeRedemptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivationCodeRedemptions",
                columns: table => new
                {
                    Serial = table.Column<int>(type: "integer", nullable: false),
                    DeviceUniqueId = table.Column<string>(type: "text", nullable: false),
                    ActivationCode = table.Column<string>(type: "text", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationCodeRedemptions", x => x.Serial);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivationCodeRedemptions");
        }
    }
}
