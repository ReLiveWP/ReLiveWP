using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Skybox.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceGuid = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    SendLocationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MPNSEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Make = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    OSVersion = table.Column<string>(type: "text", nullable: false),
                    ClientVersion = table.Column<string>(type: "text", nullable: false),
                    Capabilities = table.Column<long>(type: "bigint", nullable: false),
                    LCID = table.Column<int>(type: "integer", nullable: false),
                    TZ = table.Column<string>(type: "text", nullable: false),
                    ColorTheme = table.Column<int>(type: "integer", nullable: false),
                    ColorAccent = table.Column<long>(type: "bigint", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    IMSI = table.Column<string>(type: "text", nullable: true),
                    MobileOperator = table.Column<string>(type: "text", nullable: true),
                    CommercializedMobileOperator = table.Column<string>(type: "text", nullable: true),
                    SimId = table.Column<string>(type: "text", nullable: true),
                    MaxWorkingSet = table.Column<int>(type: "integer", nullable: false),
                    BatteryLevel = table.Column<int>(type: "integer", nullable: false),
                    PinLocked = table.Column<bool>(type: "boolean", nullable: false),
                    SimLocked = table.Column<bool>(type: "boolean", nullable: false),
                    StorageRemaining = table.Column<long>(type: "bigint", nullable: false),
                    ScreenResolution = table.Column<string>(type: "text", nullable: true),
                    NotificationChannelUrl = table.Column<string>(type: "text", nullable: true),
                    LastLocation_Reported = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastLocation_Latitude = table.Column<double>(type: "double precision", nullable: true),
                    LastLocation_Longitude = table.Column<double>(type: "double precision", nullable: true),
                    LastLocation_Altitude = table.Column<double>(type: "double precision", nullable: true)
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
