using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.DeviceUpdate.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Updates",
                columns: table => new
                {
                    RevisionId = table.Column<long>(type: "bigint", nullable: false),
                    UpdateId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    UpdateType = table.Column<int>(type: "integer", nullable: false),
                    IsLeaf = table.Column<bool>(type: "boolean", nullable: false),
                    DeploymentAction = table.Column<string>(type: "text", nullable: false),
                    IsBundle = table.Column<bool>(type: "boolean", nullable: false),
                    LastChangeTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Updates", x => x.RevisionId);
                });

            migrationBuilder.CreateTable(
                name: "Bundles",
                columns: table => new
                {
                    BundleRevisionId = table.Column<long>(type: "bigint", nullable: false),
                    BundledUpdateId = table.Column<Guid>(type: "uuid", nullable: false),
                    BundledRevisionNumber = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bundles", x => new { x.BundleRevisionId, x.BundledUpdateId });
                    table.ForeignKey(
                        name: "FK_Bundles_Updates_BundleRevisionId",
                        column: x => x.BundleRevisionId,
                        principalTable: "Updates",
                        principalColumn: "RevisionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtendedMetadata",
                columns: table => new
                {
                    RevisionId = table.Column<long>(type: "bigint", nullable: false),
                    Xml = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtendedMetadata", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_ExtendedMetadata_Updates_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "Updates",
                        principalColumn: "RevisionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    UpdateRevisionId = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    DigestSha1 = table.Column<string>(type: "text", nullable: true),
                    DigestSha256 = table.Column<string>(type: "text", nullable: true),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    LocalPath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => new { x.UpdateRevisionId, x.FileName });
                    table.ForeignKey(
                        name: "FK_Files_Updates_UpdateRevisionId",
                        column: x => x.UpdateRevisionId,
                        principalTable: "Updates",
                        principalColumn: "RevisionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fragments",
                columns: table => new
                {
                    UpdateRevisionId = table.Column<long>(type: "bigint", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    FragmentType = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Xml = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fragments", x => new { x.UpdateRevisionId, x.Ordinal });
                    table.ForeignKey(
                        name: "FK_Fragments_Updates_UpdateRevisionId",
                        column: x => x.UpdateRevisionId,
                        principalTable: "Updates",
                        principalColumn: "RevisionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Localizations",
                columns: table => new
                {
                    UpdateRevisionId = table.Column<long>(type: "bigint", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MoreInfoUrl = table.Column<string>(type: "text", nullable: true),
                    SupportUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localizations", x => new { x.UpdateRevisionId, x.Language });
                    table.ForeignKey(
                        name: "FK_Localizations_Updates_UpdateRevisionId",
                        column: x => x.UpdateRevisionId,
                        principalTable: "Updates",
                        principalColumn: "RevisionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Metadata",
                columns: table => new
                {
                    RevisionId = table.Column<long>(type: "bigint", nullable: false),
                    SyncXml = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metadata", x => x.RevisionId);
                    table.ForeignKey(
                        name: "FK_Metadata_Updates_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "Updates",
                        principalColumn: "RevisionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prerequisites",
                columns: table => new
                {
                    UpdateRevisionId = table.Column<long>(type: "bigint", nullable: false),
                    PrerequisiteUpdateId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    IsCategory = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prerequisites", x => new { x.UpdateRevisionId, x.PrerequisiteUpdateId });
                    table.ForeignKey(
                        name: "FK_Prerequisites_Updates_UpdateRevisionId",
                        column: x => x.UpdateRevisionId,
                        principalTable: "Updates",
                        principalColumn: "RevisionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bundles_BundledUpdateId",
                table: "Bundles",
                column: "BundledUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_Prerequisites_PrerequisiteUpdateId",
                table: "Prerequisites",
                column: "PrerequisiteUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_Updates_UpdateId",
                table: "Updates",
                column: "UpdateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bundles");

            migrationBuilder.DropTable(
                name: "ExtendedMetadata");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Fragments");

            migrationBuilder.DropTable(
                name: "Localizations");

            migrationBuilder.DropTable(
                name: "Metadata");

            migrationBuilder.DropTable(
                name: "Prerequisites");

            migrationBuilder.DropTable(
                name: "Updates");
        }
    }
}
