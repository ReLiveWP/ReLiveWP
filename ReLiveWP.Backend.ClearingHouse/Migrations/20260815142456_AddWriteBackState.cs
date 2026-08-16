using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ClearingHouse.Migrations
{
    /// <inheritdoc />
    public partial class AddWriteBackState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastRunConflicts",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastRunWritten",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastWriteBackAt",
                table: "ContactSyncSources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Writes",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRunConflicts",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "LastRunWritten",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "LastWriteBackAt",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "Writes",
                table: "ContactSyncSources");
        }
    }
}
