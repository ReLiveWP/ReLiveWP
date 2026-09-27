using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReLiveWP.Backend.Mailbox.Migrations
{
    /// <inheritdoc />
    public partial class MimeRawToBytea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // column held Latin1-mapped octets, so LATIN1 gets the original bytes back
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ALTER COLUMN ""MimeRaw"" TYPE bytea USING convert_to(""MimeRaw"", 'LATIN1');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Items"" ALTER COLUMN ""MimeRaw"" TYPE text USING convert_from(""MimeRaw"", 'LATIN1');");
        }
    }
}
