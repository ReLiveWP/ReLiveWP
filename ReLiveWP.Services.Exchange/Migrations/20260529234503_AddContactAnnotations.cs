using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Services.Exchange.Migrations
{
    /// <inheritdoc />
    public partial class AddContactAnnotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemAnnotation");

            migrationBuilder.CreateTable(
                name: "ContactAnnotations",
                columns: table => new
                {
                    ContactItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Cid = table.Column<long>(type: "INTEGER", nullable: true),
                    ObjectId = table.Column<string>(type: "TEXT", nullable: true),
                    WLId = table.Column<string>(type: "TEXT", nullable: true),
                    ImMri = table.Column<string>(type: "TEXT", nullable: true),
                    ContactType = table.Column<string>(type: "TEXT", nullable: true),
                    UserTileUrl = table.Column<string>(type: "TEXT", nullable: true),
                    UserTileHash = table.Column<string>(type: "TEXT", nullable: true),
                    TrustLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    FavoriteOrder = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactAnnotations", x => x.ContactItemId);
                    table.ForeignKey(
                        name: "FK_ContactAnnotations_Items_ContactItemId",
                        column: x => x.ContactItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactAnnotations");

            migrationBuilder.CreateTable(
                name: "ItemAnnotation",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    ValueType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemAnnotation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemAnnotation_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemAnnotation_ItemId",
                table: "ItemAnnotation",
                column: "ItemId");
        }
    }
}
