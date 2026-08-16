using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ClearingHouse.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncRunStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastRunCreated",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastRunDeleted",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LastRunRefused",
                table: "ContactSyncSources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LastRunSkipped",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastRunUpdated",
                table: "ContactSyncSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RunRequestedAt",
                table: "ContactSyncSources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RunStartedAt",
                table: "ContactSyncSources",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRunCreated",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "LastRunDeleted",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "LastRunRefused",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "LastRunSkipped",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "LastRunUpdated",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "RunRequestedAt",
                table: "ContactSyncSources");

            migrationBuilder.DropColumn(
                name: "RunStartedAt",
                table: "ContactSyncSources");
        }
    }
}
