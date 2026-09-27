using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_ConversationId",
                table: "Items",
                columns: new[] { "UserId", "ConversationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_ServerId",
                table: "Items",
                columns: new[] { "UserId", "ServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactAnnotations_Cid",
                table: "ContactAnnotations",
                column: "Cid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_UserId_ConversationId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_UserId_ServerId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_ContactAnnotations_Cid",
                table: "ContactAnnotations");
        }
    }
}
