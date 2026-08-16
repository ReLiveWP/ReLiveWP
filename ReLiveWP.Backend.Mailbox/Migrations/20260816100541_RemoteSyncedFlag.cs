using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class RemoteSyncedFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RemoteSynced",
                table: "Items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // everything already mirrored is still following its provider; without this they all read
            // as edited and quietly stop updating
            migrationBuilder.Sql(
                """UPDATE "Items" SET "RemoteSynced" = true WHERE "OriginServiceId" IS NOT NULL;""");

            migrationBuilder.DropColumn(
                name: "OriginPayload",
                table: "Items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemoteSynced",
                table: "Items");

            migrationBuilder.AddColumn<string>(
                name: "OriginPayload",
                table: "Items",
                type: "text",
                nullable: true);
        }
    }
}
