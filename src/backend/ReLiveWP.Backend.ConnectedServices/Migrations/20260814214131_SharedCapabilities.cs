using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ConnectedServices.Migrations
{
    /// <inheritdoc />
    public partial class SharedCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SharedCapabilities",
                table: "ConnectedServices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SharedCapabilities",
                table: "ConnectedServices");
        }
    }
}
