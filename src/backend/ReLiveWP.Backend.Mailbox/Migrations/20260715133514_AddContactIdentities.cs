using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class AddContactIdentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactIdentities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ContactItemId = table.Column<string>(type: "text", nullable: false),
                    ContactCid = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactIdentities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactIdentities_Items_ContactItemId",
                        column: x => x.ContactItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactIdentities_ContactItemId",
                table: "ContactIdentities",
                column: "ContactItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactIdentities_UserId_ContactCid",
                table: "ContactIdentities",
                columns: new[] { "UserId", "ContactCid" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactIdentities_UserId_Provider_ExternalId",
                table: "ContactIdentities",
                columns: new[] { "UserId", "Provider", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactIdentities");
        }
    }
}
