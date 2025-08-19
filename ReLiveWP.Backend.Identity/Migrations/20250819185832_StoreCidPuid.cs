using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Identity.Migrations
{
    /// <inheritdoc />
    public partial class StoreCidPuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cid",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Puid",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Cid",
                table: "AspNetUsers",
                column: "Cid");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Puid",
                table: "AspNetUsers",
                column: "Puid");
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

            migrationBuilder.DropColumn(
                name: "Cid",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Puid",
                table: "AspNetUsers");
        }
    }
}
