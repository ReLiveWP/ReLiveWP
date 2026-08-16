using System.Text;
using ReLiveWP.Services.Activity.Services;

namespace ReLiveWP.Services.Activity.Tests;

public class PhotoUploadReaderTests
{
    private const string Boundary = "MIMEBoundary";
    private const string UserId = "434e23de-f2c1-4ec2-95fe-5d7a6d8d65ff";

    private static Stream Body(string? entry, string contentType = "image/jpeg", string content = "not really a jpeg")
    {
        var buffer = new StringBuilder();

        if (entry != null)
        {
            buffer.Append($"--{Boundary}\r\n")
                  .Append("Content-Type: application/atom+xml\r\n\r\n")
                  .Append(entry)
                  .Append("\r\n");
        }

        buffer.Append($"--{Boundary}\r\n")
              .Append($"Content-Type: {contentType}\r\n\r\n")
              .Append(content)
              .Append($"\r\n--{Boundary}--\r\n");

        return new MemoryStream(Encoding.UTF8.GetBytes(buffer.ToString()));
    }

    private static string Entry(string type = "photo", string? title = "WP_000084.jpg", string resolveNameConflict = "true")
        => $"""
            <entry xmlns="http://www.w3.org/2005/Atom" xmlns:live="http://api.live.com/schemas">
              {(title == null ? "" : $"<title>{title}</title>")}
              <summary>from the phone</summary>
              <live:type>{type}</live:type>
              <live:ResolveNameConflict>{resolveNameConflict}</live:ResolveNameConflict>
              <live:SuppressNotification>1</live:SuppressNotification>
            </entry>
            """;

    private static async Task<PhotoUpload> ReadAsync(Stream body)
        => await PhotoUploadReader.ReadAsync(body, Boundary, UserId, "wmphotos")
           ?? throw new InvalidOperationException("the reader found nothing to upload");

    [Fact]
    public async Task TheAtomSectionNamesThePhoto()
    {
        await using var upload = await ReadAsync(Body(Entry()));

        Assert.Equal("WP_000084.jpg", upload.Metadata.FileName);
        Assert.Equal("from the phone", upload.Metadata.Summary);
        Assert.Equal("wmphotos", upload.Metadata.Category);
        Assert.Equal(UserId, upload.Metadata.UserId);
        Assert.Equal("image/jpeg", upload.Metadata.ContentType);
        Assert.Equal("photo", upload.Metadata.MediaType);
        Assert.True(upload.Spool.Length > 0);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    public async Task LiveBooleansTakeBothSpellings(string value, bool expected)
    {
        await using var upload = await ReadAsync(Body(Entry(resolveNameConflict: value)));

        Assert.Equal(expected, upload.Metadata.ResolveNameConflict);
        Assert.True(upload.Metadata.SuppressNotification);
    }

    [Fact]
    public async Task AnUntitledPhotoGetsAGeneratedName()
    {
        await using var photo = await ReadAsync(Body(Entry(title: null)));
        await using var video = await ReadAsync(Body(Entry("video", title: null), "video/mp4"));

        Assert.EndsWith(".jpg", photo.Metadata.FileName);
        Assert.EndsWith(".mp4", video.Metadata.FileName);
        Assert.Equal("video", video.Metadata.MediaType);
    }

    // some sends label the payload octet-stream, and the device would not play back what that implies
    [Fact]
    public async Task AnUnusableContentTypeFallsBackToTheMediaType()
    {
        await using var photo = await ReadAsync(Body(Entry(), contentType: "binary"));
        await using var video = await ReadAsync(Body(Entry("video"), contentType: "binary"));

        Assert.Equal("image/jpeg", photo.Metadata.ContentType);
        Assert.Equal("video/mp4", video.Metadata.ContentType);
    }

    [Fact]
    public async Task APostWithNoPayloadIsNothingToUpload()
    {
        Assert.Null(await PhotoUploadReader.ReadAsync(Body(Entry(), content: ""), Boundary, UserId, "wmphotos"));
    }

    [Theory]
    [InlineData("multipart/related; boundary=\"MIMEBoundary\"", "MIMEBoundary")]
    [InlineData("Multipart/Related; type=\"application/atom+xml\"; boundary=MIMEBoundary", "MIMEBoundary")]
    public void AMultipartRelatedPostCarriesItsBoundary(string contentType, string expected)
    {
        Assert.True(PhotoUploadReader.IsMultipartRelated(contentType, out var boundary));
        Assert.Equal(expected, boundary);
    }

    [Theory]
    [InlineData("application/atom+xml")]
    [InlineData("multipart/form-data; boundary=x")]
    [InlineData(null)]
    public void AnythingElseIsNotAnUpload(string? contentType)
    {
        Assert.False(PhotoUploadReader.IsMultipartRelated(contentType, out _));
    }
}
