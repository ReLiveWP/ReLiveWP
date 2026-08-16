using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class ContactLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LinkIsManual",
                table: "ContactAnnotations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ContactEmails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ContactItemId = table.Column<string>(type: "text", nullable: false),
                    NormalizedAddress = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactEmails_Items_ContactItemId",
                        column: x => x.ContactItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactEmails_ContactItemId",
                table: "ContactEmails",
                column: "ContactItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactEmails_NormalizedAddress",
                table: "ContactEmails",
                column: "NormalizedAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ContactEmails_UserId_NormalizedAddress",
                table: "ContactEmails",
                columns: new[] { "UserId", "NormalizedAddress" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactEmails");

            migrationBuilder.DropColumn(
                name: "LinkIsManual",
                table: "ContactAnnotations");
        }
    }
}
