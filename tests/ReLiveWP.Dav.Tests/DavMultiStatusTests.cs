using ReLiveWP.Dav;

namespace ReLiveWP.Dav.Tests;

// The three servers differ in namespace prefix, href form and how they report content types.
// Getting any of these wrong silently yields an empty album rather than an error.
public class DavMultiStatusTests
{
    private const string Album = "Windows phone photos";

    // lowercase d: prefix, root relative hrefs, and a second propstat carrying the 404 properties
    private const string Nextcloud = """
        <?xml version="1.0"?>
        <d:multistatus xmlns:d="DAV:" xmlns:s="http://sabredav.org/ns" xmlns:oc="http://owncloud.org/ns">
          <d:response>
            <d:href>/remote.php/dav/files/me/Windows%20phone%20photos/</d:href>
            <d:propstat>
              <d:prop>
                <d:resourcetype><d:collection/></d:resourcetype>
                <d:getlastmodified>Mon, 12 Jan 2026 10:00:00 GMT</d:getlastmodified>
              </d:prop>
              <d:status>HTTP/1.1 200 OK</d:status>
            </d:propstat>
            <d:propstat>
              <d:prop><d:getcontenttype/><d:getcontentlength/><d:getetag/></d:prop>
              <d:status>HTTP/1.1 404 Not Found</d:status>
            </d:propstat>
          </d:response>
          <d:response>
            <d:href>/remote.php/dav/files/me/Windows%20phone%20photos/IMG_0001.jpg</d:href>
            <d:propstat>
              <d:prop>
                <d:resourcetype/>
                <d:getcontenttype>image/jpeg</d:getcontenttype>
                <d:getcontentlength>204800</d:getcontentlength>
                <d:getlastmodified>Tue, 13 Jan 2026 09:30:00 GMT</d:getlastmodified>
                <d:getetag>&quot;abc123&quot;</d:getetag>
              </d:prop>
              <d:status>HTTP/1.1 200 OK</d:status>
            </d:propstat>
          </d:response>
        </d:multistatus>
        """;

    // uppercase D: prefix, absolute hrefs, and a DAV: bound prefix that isn't "D"
    private const string ApacheModDav = """
        <?xml version="1.0" encoding="utf-8"?>
        <D:multistatus xmlns:D="DAV:">
          <D:response xmlns:lp1="DAV:" xmlns:lp2="http://apache.org/dav/props/">
            <D:href>http://dav.example.com/photos/Windows%20phone%20photos/</D:href>
            <D:propstat>
              <D:prop><lp1:resourcetype><D:collection/></lp1:resourcetype></D:prop>
              <D:status>HTTP/1.1 200 OK</D:status>
            </D:propstat>
          </D:response>
          <D:response xmlns:lp1="DAV:">
            <D:href>http://dav.example.com/photos/Windows%20phone%20photos/DSC_1.JPG</D:href>
            <D:propstat>
              <D:prop>
                <lp1:resourcetype/>
                <lp1:getcontenttype>application/octet-stream</lp1:getcontenttype>
                <lp1:getcontentlength>91234</lp1:getcontentlength>
                <lp1:getlastmodified>Wed, 14 Jan 2026 08:00:00 GMT</lp1:getlastmodified>
              </D:prop>
              <D:status>HTTP/1.1 200 OK</D:status>
            </D:propstat>
          </D:response>
        </D:multistatus>
        """;

    // default namespace with no prefix at all, and no trailing slash on the collection
    private const string Rclone = """
        <?xml version="1.0" encoding="UTF-8"?>
        <multistatus xmlns="DAV:">
          <response>
            <href>/Windows%20phone%20photos</href>
            <propstat>
              <prop><resourcetype><collection/></resourcetype></prop>
              <status>HTTP/1.1 200 OK</status>
            </propstat>
          </response>
          <response>
            <href>/Windows%20phone%20photos/clip.mp4</href>
            <propstat>
              <prop>
                <resourcetype/>
                <getcontenttype>video/mp4</getcontenttype>
                <getcontentlength>5242880</getcontentlength>
                <getlastmodified>Thu, 15 Jan 2026 12:00:00 GMT</getlastmodified>
              </prop>
              <status>HTTP/1.1 200 OK</status>
            </propstat>
          </response>
          <response>
            <href>/Windows%20phone%20photos/notes.txt</href>
            <propstat>
              <prop>
                <resourcetype/>
                <getcontenttype>text/plain</getcontenttype>
                <getcontentlength>12</getcontentlength>
              </prop>
              <status>HTTP/1.1 200 OK</status>
            </propstat>
          </response>
        </multistatus>
        """;

    [Theory]
    [InlineData(Nextcloud, "/remote.php/dav/files/me/Windows phone photos/IMG_0001.jpg", "IMG_0001.jpg")]
    [InlineData(ApacheModDav, "/photos/Windows phone photos/DSC_1.JPG", "DSC_1.JPG")]
    [InlineData(Rclone, "/Windows phone photos/clip.mp4", "clip.mp4")]
    public void ParsesFilesFromEveryServerDialect(string xml, string expectedPath, string expectedRelative)
    {
        var file = DavMultiStatus.Parse(xml).Responses.Single(e => !e.IsCollection && e.Path == expectedPath);

        Assert.Equal(expectedRelative, DavPath.RelativeTo(file.Path, Album));
    }

    [Theory]
    [InlineData(Nextcloud)]
    [InlineData(ApacheModDav)]
    [InlineData(Rclone)]
    public void IdentifiesTheCollectionItself(string xml)
        => Assert.Single(DavMultiStatus.Parse(xml).Responses, e => e.IsCollection);

    [Fact]
    public void SkipsPropertiesReportedAsNotFound()
    {
        var file = DavMultiStatus.Parse(Nextcloud).Responses.Single(e => !e.IsCollection);

        Assert.Equal("image/jpeg", file.ContentType);
        Assert.Equal(204800, file.Length);
        Assert.Equal("\"abc123\"", file.ETag);
    }

    [Fact]
    public void ReadsCollectionWithoutContentTypeAsNull()
        => Assert.Null(DavMultiStatus.Parse(Nextcloud).Responses.Single(e => e.IsCollection).ContentType);

    [Fact]
    public void ParsesRfc1123Timestamps()
    {
        var file = DavMultiStatus.Parse(Rclone).Responses.Single(e => e.Path.EndsWith("clip.mp4"));

        Assert.Equal(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero), file.Modified);
    }

    [Fact]
    public void MissingTimestampFallsBackToEpoch()
    {
        var file = DavMultiStatus.Parse(Rclone).Responses.Single(e => e.Path.EndsWith("notes.txt"));

        Assert.Equal(DateTimeOffset.UnixEpoch, file.Modified);
    }

    [Fact]
    public void MalformedXmlThrows()
    {
        Assert.Throws<DavParseException>(() => DavMultiStatus.Parse("""<multistatus xmlns="DAV:"><response>"""));
        Assert.Throws<DavParseException>(() => DavMultiStatus.Parse(""));
    }

    [Fact]
    public void NonMultiStatusRootThrows()
    {
        var error = Assert.Throws<DavParseException>(
            () => DavMultiStatus.Parse("""<error xmlns="DAV:"><valid-sync-token/></error>"""));

        Assert.Contains("expected multistatus", error.Message);
    }

    [Fact]
    public void OneUnaddressableResponseIsReportedRatherThanSinkingTheBatch()
    {
        var result = DavMultiStatus.Parse("""
            <multistatus xmlns="DAV:">
              <response>
                <propstat><prop><resourcetype/></prop><status>HTTP/1.1 200 OK</status></propstat>
              </response>
              <response>
                <href>/photos/good.jpg</href>
                <propstat><prop><getcontenttype>image/jpeg</getcontenttype></prop><status>HTTP/1.1 200 OK</status></propstat>
              </response>
            </multistatus>
            """);

        Assert.Equal(1, result.Skipped);
        Assert.Equal("/photos/good.jpg", Assert.Single(result.Responses).Path);
    }

    [Fact]
    public void AnUnreadableTimestampDegradesOnlyThatEntry()
    {
        var file = DavMultiStatus.Parse("""
            <multistatus xmlns="DAV:">
              <response>
                <href>/photos/odd.jpg</href>
                <propstat>
                  <prop>
                    <getcontenttype>image/jpeg</getcontenttype>
                    <getlastmodified>whenever, really</getlastmodified>
                  </prop>
                  <status>HTTP/1.1 200 OK</status>
                </propstat>
              </response>
            </multistatus>
            """).Responses.Single();

        Assert.Equal("image/jpeg", file.ContentType);
        Assert.Equal(DateTimeOffset.UnixEpoch, file.Modified);
    }

    [Fact]
    public void ReadsResponseLevelNotFoundForDeletions()
    {
        var result = DavMultiStatus.Parse("""
            <multistatus xmlns="DAV:">
              <sync-token>http://example.com/ns/sync/42</sync-token>
              <response>
                <href>/books/default/gone.vcf</href>
                <status>HTTP/1.1 404 Not Found</status>
              </response>
              <response>
                <href>/books/default/here.vcf</href>
                <propstat><prop><getetag>"e1"</getetag></prop><status>HTTP/1.1 200 OK</status></propstat>
              </response>
            </multistatus>
            """);

        Assert.Equal("http://example.com/ns/sync/42", result.SyncToken);
        Assert.Equal("/books/default/gone.vcf", Assert.Single(result.NotFound).Path);
        Assert.Equal("/books/default/here.vcf", Assert.Single(result.Found).Path);
    }

    // a propstat with no status at all is malformed, but enough servers send one that dropping the
    // properties would mean reading nothing off them
    [Fact]
    public void PropstatWithoutAStatusIsTreatedAsOk()
    {
        var file = DavMultiStatus.Parse("""
            <multistatus xmlns="DAV:">
              <response>
                <href>/photos/loose.jpg</href>
                <propstat><prop><getcontenttype>image/png</getcontenttype></prop></propstat>
              </response>
            </multistatus>
            """).Responses.Single();

        Assert.Equal("image/png", file.ContentType);
    }
}
