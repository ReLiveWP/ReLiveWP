using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ClearingHouse.Migrations
{
    /// <inheritdoc />
    public partial class AddLastAttemptedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptedAt",
                table: "ContactSyncSources",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAttemptedAt",
                table: "ContactSyncSources");
        }
    }
}
