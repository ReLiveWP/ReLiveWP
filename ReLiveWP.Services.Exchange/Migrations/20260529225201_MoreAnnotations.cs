using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Services.Exchange.Migrations
{
    /// <inheritdoc />
    public partial class MoreAnnotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ValueType",
                table: "ItemAnnotation",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Folders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "Folders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValueType",
                table: "ItemAnnotation");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Folders");
        }
    }
}
