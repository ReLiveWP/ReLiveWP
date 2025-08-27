using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Identity.Migrations
{
    /// <inheritdoc />
    public partial class DPoPKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DPoPKeys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DPoPKeys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedServices_DPoPKeyId",
                table: "ConnectedServices",
                column: "DPoPKeyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectedServices_DPoPKeys_DPoPKeyId",
                table: "ConnectedServices",
                column: "DPoPKeyId",
                principalTable: "DPoPKeys",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConnectedServices_DPoPKeys_DPoPKeyId",
                table: "ConnectedServices");

            migrationBuilder.DropTable(
                name: "DPoPKeys");

            migrationBuilder.DropIndex(
                name: "IX_ConnectedServices_DPoPKeyId",
                table: "ConnectedServices");
        }
    }
}
