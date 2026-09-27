using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class AddItemClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_CollectionId_ClientId",
                table: "Items",
                columns: new[] { "UserId", "CollectionId", "ClientId" },
                unique: true,
                filter: "\"ClientId\" IS NOT NULL AND \"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_UserId_CollectionId_ClientId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Items");
        }
    }
}
