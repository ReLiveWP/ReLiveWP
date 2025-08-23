using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Identity.Migrations
{
    /// <inheritdoc />
    public partial class OAuthPhase4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConnectedServices_AspNetUsers_LiveUserId",
                table: "ConnectedServices");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingOAuths_AspNetUsers_LiveUserId",
                table: "PendingOAuths");

            migrationBuilder.DropIndex(
                name: "IX_PendingOAuths_LiveUserId",
                table: "PendingOAuths");

            migrationBuilder.DropIndex(
                name: "IX_ConnectedServices_LiveUserId",
                table: "ConnectedServices");

            migrationBuilder.DropColumn(
                name: "LiveUserId",
                table: "PendingOAuths");

            migrationBuilder.DropColumn(
                name: "LiveUserId",
                table: "ConnectedServices");

            migrationBuilder.CreateIndex(
                name: "IX_PendingOAuths_UserId",
                table: "PendingOAuths",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedServices_UserId",
                table: "ConnectedServices",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectedServices_AspNetUsers_UserId",
                table: "ConnectedServices",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingOAuths_AspNetUsers_UserId",
                table: "PendingOAuths",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConnectedServices_AspNetUsers_UserId",
                table: "ConnectedServices");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingOAuths_AspNetUsers_UserId",
                table: "PendingOAuths");

            migrationBuilder.DropIndex(
                name: "IX_PendingOAuths_UserId",
                table: "PendingOAuths");

            migrationBuilder.DropIndex(
                name: "IX_ConnectedServices_UserId",
                table: "ConnectedServices");

            migrationBuilder.AddColumn<Guid>(
                name: "LiveUserId",
                table: "PendingOAuths",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LiveUserId",
                table: "ConnectedServices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingOAuths_LiveUserId",
                table: "PendingOAuths",
                column: "LiveUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedServices_LiveUserId",
                table: "ConnectedServices",
                column: "LiveUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectedServices_AspNetUsers_LiveUserId",
                table: "ConnectedServices",
                column: "LiveUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PendingOAuths_AspNetUsers_LiveUserId",
                table: "PendingOAuths",
                column: "LiveUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
