using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Identity.Migrations
{
    /// <inheritdoc />
    public partial class SignupAccountFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlternateEmail",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignupActivationCodeHash",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SignupActivationCodeHash",
                table: "AspNetUsers",
                column: "SignupActivationCodeHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SignupActivationCodeHash",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AlternateEmail",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SignupActivationCodeHash",
                table: "AspNetUsers");
        }
    }
}
