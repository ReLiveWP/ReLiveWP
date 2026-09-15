using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.DeviceUpdate.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuthoredCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLeafReported",
                table: "Updates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Origin",
                table: "Updates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Every existing row came off the wire, so what upstream reported is what is stored.
            // Without this the recompute reads false for all of them and marks the catalog non-leaf.
            migrationBuilder.Sql(@"UPDATE ""Updates"" SET ""IsLeafReported"" = ""IsLeaf"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLeafReported",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Updates");
        }
    }
}
