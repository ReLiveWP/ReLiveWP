using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Skybox.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceGuid = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SendLocationEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MPNSEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Make = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    FriendlyName = table.Column<string>(type: "TEXT", nullable: true),
                    OSVersion = table.Column<string>(type: "TEXT", nullable: false),
                    ClientVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Capabilities = table.Column<uint>(type: "INTEGER", nullable: false),
                    LCID = table.Column<int>(type: "INTEGER", nullable: false),
                    TZ = table.Column<string>(type: "TEXT", nullable: false),
                    ColorTheme = table.Column<int>(type: "INTEGER", nullable: false),
                    ColorAccent = table.Column<uint>(type: "INTEGER", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    IMSI = table.Column<string>(type: "TEXT", nullable: true),
                    MobileOperator = table.Column<string>(type: "TEXT", nullable: true),
                    CommercializedMobileOperator = table.Column<string>(type: "TEXT", nullable: true),
                    SimId = table.Column<string>(type: "TEXT", nullable: true),
                    MaxWorkingSet = table.Column<int>(type: "INTEGER", nullable: false),
                    BatteryLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    PinLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    SimLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    StorageRemaining = table.Column<long>(type: "INTEGER", nullable: false),
                    ScreenResolution = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceGuid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
