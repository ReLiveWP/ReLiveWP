using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Identity.Migrations
{
    /// <inheritdoc />
    public partial class SignupInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InviteSerial",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_InviteSerial",
                table: "AspNetUsers",
                column: "InviteSerial",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_InviteSerial",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InviteSerial",
                table: "AspNetUsers");
        }
    }
}
