using ReLiveWP.Backend.SkyDrive.Services;
using ReLiveWP.Dav;

namespace ReLiveWP.Backend.SkyDrive.Tests;

// The share root is not something this client is told, so paths are derived by anchoring on the
// collection the PROPFIND asked for. Getting it wrong yields plausible but unopenable paths.
public class WebDavFileSyncTests
{
    // nextcloud: a deep server prefix that is no part of the share-relative path
    private const string Nextcloud = """
        <?xml version="1.0"?>
        <d:multistatus xmlns:d="DAV:">
          <d:response>
            <d:href>/remote.php/dav/files/me/Documents/</d:href>
            <d:propstat><d:prop><d:resourcetype><d:collection/></d:resourcetype></d:prop><d:status>HTTP/1.1 200 OK</d:status></d:propstat>
          </d:response>
          <d:response>
            <d:href>/remote.php/dav/files/me/Documents/Budget%202026.xlsx</d:href>
            <d:propstat>
              <d:prop>
                <d:resourcetype/>
                <d:getcontentlength>4096</d:getcontentlength>
                <d:getlastmodified>Tue, 13 Jan 2026 09:30:00 GMT</d:getlastmodified>
                <d:getetag>&quot;x1&quot;</d:getetag>
              </d:prop>
              <d:status>HTTP/1.1 200 OK</d:status>
            </d:propstat>
          </d:response>
          <d:response>
            <d:href>/remote.php/dav/files/me/Documents/Notes/</d:href>
            <d:propstat><d:prop><d:resourcetype><d:collection/></d:resourcetype></d:prop><d:status>HTTP/1.1 200 OK</d:status></d:propstat>
          </d:response>
        </d:multistatus>
        """;

    // apache: fully qualified hrefs
    private const string ApacheRoot = """
        <?xml version="1.0" encoding="utf-8"?>
        <D:multistatus xmlns:D="DAV:">
          <D:response>
            <D:href>http://dav.example.com/share/</D:href>
            <D:propstat><D:prop><D:resourcetype><D:collection/></D:resourcetype></D:prop><D:status>HTTP/1.1 200 OK</D:status></D:propstat>
          </D:response>
          <D:response>
            <D:href>http://dav.example.com/share/Documents/</D:href>
            <D:propstat><D:prop><D:resourcetype><D:collection/></D:resourcetype></D:prop><D:status>HTTP/1.1 200 OK</D:status></D:propstat>
          </D:response>
          <D:response>
            <D:href>http://dav.example.com/share/readme.txt</D:href>
            <D:propstat>
              <D:prop><D:resourcetype/><D:getcontentlength>12</D:getcontentlength></D:prop>
              <D:status>HTTP/1.1 200 OK</D:status>
            </D:propstat>
          </D:response>
        </D:multistatus>
        """;

    private static IReadOnlyList<ProviderEntry> Children(string xml, string requestPath)
        => WebDavFileSyncProxyClient.Children(DavMultiStatus.Parse(xml), requestPath);

    [Fact]
    public void PathsAreRelativeToTheShareNotTheServer()
    {
        var entries = Children(Nextcloud, "Documents");

        Assert.Equal(
            ["Documents/Budget 2026.xlsx", "Documents/Notes"],
            entries.Select(e => e.Path).Order());
    }

    [Fact]
    public void TheCollectionItselfIsNotOneOfItsChildren()
        => Assert.DoesNotContain(Children(Nextcloud, "Documents"), e => e.Path == "Documents");

    [Fact]
    public void ListingTheRootDoesNotLeadPathsWithASlash()
    {
        var entries = Children(ApacheRoot, "");

        Assert.Equal(["Documents", "readme.txt"], entries.Select(e => e.Path).Order());
    }

    [Fact]
    public void FoldersAndFilesAreDistinguished()
    {
        var entries = Children(Nextcloud, "Documents");

        Assert.True(entries.Single(e => e.Name == "Notes").IsFolder);
        Assert.False(entries.Single(e => e.Name == "Budget 2026.xlsx").IsFolder);
    }

    [Fact]
    public void FileMetadataSurvivesTheProjection()
    {
        var file = Children(Nextcloud, "Documents").Single(e => !e.IsFolder);

        Assert.Equal("Budget 2026.xlsx", file.Name);
        Assert.Equal(4096, file.Size);
        Assert.Equal("\"x1\"", file.ETag);
        Assert.Equal(new DateTimeOffset(2026, 1, 13, 9, 30, 0, TimeSpan.Zero), file.Modified);
    }

    // servers routinely report octet-stream for everything, and the Office hub cares
    [Fact]
    public void OfficeContentTypesFallBackToTheExtension()
    {
        var file = Children(Nextcloud, "Documents").Single(e => !e.IsFolder);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
    }

    [Fact]
    public void ResourceIdsRoundTripToTheSharePath()
    {
        var file = Children(Nextcloud, "Documents").Single(e => !e.IsFolder);

        Assert.True(WebDavItemId.TryDecode(file.ResourceId, out var path));
        Assert.Equal("Documents/Budget 2026.xlsx", path);
    }

    [Fact]
    public void AnEmptyListingYieldsNothingRatherThanThrowing()
        => Assert.Empty(Children("""<multistatus xmlns="DAV:"/>""", "Documents"));
}
