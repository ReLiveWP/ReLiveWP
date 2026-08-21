using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReLiveWP.Mail.Tests;

public class MimeIngestTests
{
    private const string Sender = "wam@relivewp.net";

    private static readonly MimeIngest Ingest = new(NullLogger<MimeIngest>.Instance);

    private static byte[] Raw(string mime) => Encoding.Latin1.GetBytes(mime);

    [Fact]
    public void Parses_headers_and_a_plain_body()
    {
        var item = Ingest.ToEmailItem(Raw(
            "From: someone@example.com\r\n" +
            "To: ada@relivewp.net\r\n" +
            "Cc: grace@relivewp.net\r\n" +
            "Subject: hello there\r\n" +
            "\r\n" +
            "body text\r\n"), Sender);

        Assert.Equal("hello there", item.Subject);
        Assert.Contains("ada@relivewp.net", item.To);
        Assert.Contains("grace@relivewp.net", item.Cc);
        Assert.Equal("body text", item.Body.Trim());
        Assert.Equal(1, item.BodyType);
        Assert.Equal("IPM.Note", item.MessageClass);
    }

    [Fact]
    public void The_supplied_address_wins_over_the_From_header()
    {
        var item = Ingest.ToEmailItem(Raw(
            "From: spoofed@example.com\r\nTo: ada@relivewp.net\r\n\r\nhi\r\n"), Sender);

        Assert.Equal(Sender, item.From);
    }

    [Fact]
    public void An_empty_address_falls_back_to_the_From_header()
    {
        var item = Ingest.ToEmailItem(Raw(
            "From: someone@example.com\r\nTo: ada@relivewp.net\r\n\r\nhi\r\n"), "");

        Assert.Contains("someone@example.com", item.From);
    }

    [Fact]
    public void Bcc_is_captured_on_the_item()
    {
        var item = Ingest.ToEmailItem(Raw(
            "From: a@b\r\nTo: ada@relivewp.net\r\nBcc: hidden@relivewp.net\r\n\r\nhi\r\n"), Sender);

        Assert.Contains("hidden@relivewp.net", item.Bcc);
    }

    [Fact]
    public void Html_wins_when_the_message_carries_both_bodies()
    {
        var item = Ingest.ToEmailItem(Raw(
            "From: a@b\r\nTo: c@d\r\nSubject: s\r\n" +
            "Content-Type: multipart/alternative; boundary=\"bnd\"\r\n" +
            "\r\n" +
            "--bnd\r\nContent-Type: text/plain\r\n\r\nplain version\r\n" +
            "--bnd\r\nContent-Type: text/html\r\n\r\n<p>html version</p>\r\n" +
            "--bnd--\r\n"), Sender);

        Assert.Equal(2, item.BodyType);
        Assert.Equal(2, item.NativeBodyType);
        Assert.Contains("html version", item.Body);
    }

    [Fact]
    public void Raw_bytes_are_kept_verbatim_including_eight_bit_content()
    {
        // 0xfe/0xff never appear in valid UTF-8, so a UTF-8 round trip would mangle them
        var raw = Raw("From: a@b\r\nTo: c@d\r\nSubject: s\r\n\r\n")
            .Concat<byte>([0xfe, 0xff, 0x80, 0x0d, 0x0a])
            .ToArray();

        var item = Ingest.ToEmailItem(raw, Sender);

        Assert.Equal(raw, item.MimeRaw.ToByteArray());
    }

    [Fact]
    public void Attachments_come_back_with_their_decoded_bytes()
    {
        var payload = "hello attachment"u8.ToArray();
        var item = Ingest.ToEmailItem(Raw(
            "From: a@b\r\nTo: c@d\r\nSubject: s\r\n" +
            "Content-Type: multipart/mixed; boundary=\"bnd\"\r\n" +
            "\r\n" +
            "--bnd\r\nContent-Type: text/plain\r\n\r\nsee attached\r\n" +
            "--bnd\r\nContent-Type: application/octet-stream\r\n" +
            "Content-Disposition: attachment; filename=\"note.bin\"\r\n" +
            "Content-Transfer-Encoding: base64\r\n\r\n" +
            Convert.ToBase64String(payload) + "\r\n" +
            "--bnd--\r\n"), Sender);

        var attachment = Assert.Single(item.Attachments);
        Assert.Equal("note.bin", attachment.DisplayName);
        Assert.Equal(payload.Length, attachment.EstimatedDataSize);
        Assert.Equal(payload, attachment.Content.ToByteArray());
        Assert.False(attachment.IsInline);
    }

    [Fact]
    public void Unparseable_input_still_keeps_the_raw_blob_and_the_sender()
    {
        var raw = new byte[] { 0x00, 0xff, 0xfe, 0x01 };

        var item = Ingest.ToEmailItem(raw, Sender);

        Assert.Equal(Sender, item.From);
        Assert.Equal(raw, item.MimeRaw.ToByteArray());
    }
}
