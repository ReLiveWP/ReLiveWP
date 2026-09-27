using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSyncResponseBlobWithCheckpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncResponses");

            migrationBuilder.AddColumn<string>(
                name: "PreviousSyncKey",
                table: "SyncStates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "PreviousWatermark",
                table: "SyncStates",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousSyncKey",
                table: "SyncStates");

            migrationBuilder.DropColumn(
                name: "PreviousWatermark",
                table: "SyncStates");

            migrationBuilder.CreateTable(
                name: "SyncResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollectionId = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    Response = table.Column<string>(type: "text", nullable: false),
                    SyncKey = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Watermark = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncResponses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncResponses_UserId_DeviceId_CollectionId",
                table: "SyncResponses",
                columns: new[] { "UserId", "DeviceId", "CollectionId" },
                unique: true);
        }
    }
}
