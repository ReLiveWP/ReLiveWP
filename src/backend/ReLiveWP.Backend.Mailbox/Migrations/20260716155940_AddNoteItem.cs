using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note_Body",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Note_LastModifiedDate",
                table: "Items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note_MessageClass",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Note_NativeBodyType",
                table: "Items",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note_Subject",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NoteCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    NoteItemId = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteCategories_Items_NoteItemId",
                        column: x => x.NoteItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoteCategories_NoteItemId",
                table: "NoteCategories",
                column: "NoteItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoteCategories");

            migrationBuilder.DropColumn(
                name: "Note_Body",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Note_LastModifiedDate",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Note_MessageClass",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Note_NativeBodyType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Note_Subject",
                table: "Items");
        }
    }
}
