using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Skybox.Migrations
{
    /// <inheritdoc />
    public partial class LastLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LastLocation_Altitude",
                table: "Devices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastLocation_Latitude",
                table: "Devices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastLocation_Longitude",
                table: "Devices",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLocation_Reported",
                table: "Devices",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLocation_Altitude",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "LastLocation_Latitude",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "LastLocation_Longitude",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "LastLocation_Reported",
                table: "Devices");
        }
    }
}
