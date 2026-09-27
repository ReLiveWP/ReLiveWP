using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.ConnectedServices.Migrations
{
    /// <inheritdoc />
    public partial class XminRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin is a postgres system column, there's nothing to create, the old counter just goes
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ConnectedServices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RowVersion",
                table: "ConnectedServices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
