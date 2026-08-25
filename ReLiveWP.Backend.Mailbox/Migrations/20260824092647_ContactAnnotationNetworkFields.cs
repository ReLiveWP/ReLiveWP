using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class ContactAnnotationNetworkFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MobileIMEnabled",
                table: "ContactAnnotations",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherMri",
                table: "ContactAnnotations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShellContactType",
                table: "ContactAnnotations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "ContactAnnotations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Networks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DomainId = table.Column<int>(type: "integer", nullable: false),
                    UserEmail = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    AccountName = table.Column<string>(type: "text", nullable: true),
                    DomainTag = table.Column<string>(type: "text", nullable: true),
                    LastSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PsaState = table.Column<string>(type: "text", nullable: true),
                    PsaLastChanged = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Offers = table.Column<int>(type: "integer", nullable: true),
                    PartnerOffers = table.Column<int>(type: "integer", nullable: true),
                    ClientToken = table.Column<string>(type: "text", nullable: true),
                    ClientToken2 = table.Column<string>(type: "text", nullable: true),
                    ClientPublishSecret = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Networks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Networks_UserId_DomainId_UserEmail",
                table: "Networks",
                columns: new[] { "UserId", "DomainId", "UserEmail" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Networks");

            migrationBuilder.DropColumn(
                name: "MobileIMEnabled",
                table: "ContactAnnotations");

            migrationBuilder.DropColumn(
                name: "OtherMri",
                table: "ContactAnnotations");

            migrationBuilder.DropColumn(
                name: "ShellContactType",
                table: "ContactAnnotations");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "ContactAnnotations");
        }
    }
}
