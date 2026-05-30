using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Identity.Migrations
{
    /// <inheritdoc />
    public partial class UniqueIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Cid",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Puid",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Cid",
                table: "AspNetUsers",
                column: "Cid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Puid",
                table: "AspNetUsers",
                column: "Puid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Cid",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Puid",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Cid",
                table: "AspNetUsers",
                column: "Cid");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Puid",
                table: "AspNetUsers",
                column: "Puid");
        }
    }
}
