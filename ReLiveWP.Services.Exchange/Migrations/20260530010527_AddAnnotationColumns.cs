using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Services.Exchange.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnotationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CachedAnnotationNames",
                table: "SyncStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "Folders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CachedAnnotationNames",
                table: "SyncStates");

            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "Folders");
        }
    }
}
