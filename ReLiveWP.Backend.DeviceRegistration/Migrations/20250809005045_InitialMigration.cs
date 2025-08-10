using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.DeviceRegistration.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UniqueId = table.Column<string>(type: "TEXT", nullable: false),
                    CertificateSubject = table.Column<string>(type: "TEXT", nullable: false),
                    OSVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Locale = table.Column<string>(type: "TEXT", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    Operator = table.Column<string>(type: "TEXT", nullable: true),
                    IMEI = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_CertificateSubject",
                table: "Devices",
                column: "CertificateSubject");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UniqueId",
                table: "Devices",
                column: "UniqueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
