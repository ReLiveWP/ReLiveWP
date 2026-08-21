using System.Text;

namespace ReLiveWP.Mail.Tests;

public class MimeSubmissionTests
{
    private const string Domain = "relivewp.net";

    private static PreparedSubmission Prepare(string mime)
    {
        Assert.True(MimeSubmission.TryPrepare(Encoding.Latin1.GetBytes(mime), Domain, out var prepared));
        return prepared;
    }

    [Fact]
    public void Recipients_gather_from_to_cc_and_bcc()
    {
        var prepared = Prepare(
            "From: wam@relivewp.net\r\n" +
            "To: ada@relivewp.net\r\n" +
            "Cc: grace@relivewp.net\r\n" +
            "Bcc: alan@relivewp.net\r\n" +
            "\r\nhi\r\n");

        Assert.Equal(
            ["ada@relivewp.net", "grace@relivewp.net", "alan@relivewp.net"],
            prepared.Recipients);
    }

    [Fact]
    public void An_address_repeated_across_headers_is_only_delivered_once()
    {
        var prepared = Prepare(
            "From: wam@relivewp.net\r\nTo: ada@relivewp.net\r\nCc: ADA@relivewp.net\r\n\r\nhi\r\n");

        Assert.Single(prepared.Recipients);
    }

    [Fact]
    public void The_delivered_copy_drops_bcc_and_the_sender_copy_keeps_it()
    {
        var prepared = Prepare(
            "From: wam@relivewp.net\r\nTo: ada@relivewp.net\r\nBcc: alan@relivewp.net\r\n\r\nhi\r\n");

        Assert.DoesNotContain("alan@relivewp.net", Encoding.Latin1.GetString(prepared.DeliveryBytes));
        Assert.Contains("alan@relivewp.net", Encoding.Latin1.GetString(prepared.SentItemsBytes));
    }

    [Fact]
    public void A_missing_message_id_is_generated_against_our_domain()
    {
        var prepared = Prepare("From: wam@relivewp.net\r\nTo: ada@relivewp.net\r\n\r\nhi\r\n");

        var delivered = Encoding.Latin1.GetString(prepared.DeliveryBytes);
        Assert.Contains("Message-Id:", delivered, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Domain, delivered);
    }

    [Fact]
    public void An_existing_message_id_is_left_alone()
    {
        var prepared = Prepare(
            "From: wam@relivewp.net\r\nTo: ada@relivewp.net\r\n" +
            "Message-Id: <keep-me@device.local>\r\n\r\nhi\r\n");

        Assert.Contains("<keep-me@device.local>", Encoding.Latin1.GetString(prepared.DeliveryBytes));
    }

    [Fact]
    public void A_missing_date_is_stamped()
    {
        var prepared = Prepare("From: wam@relivewp.net\r\nTo: ada@relivewp.net\r\n\r\nhi\r\n");

        Assert.Contains("Date:", Encoding.Latin1.GetString(prepared.DeliveryBytes), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_from_address_comes_back_for_the_ownership_check()
    {
        var prepared = Prepare(
            "From: Wam <wam@relivewp.net>\r\nTo: ada@relivewp.net\r\n\r\nhi\r\n");

        Assert.Equal("wam@relivewp.net", prepared.From);
    }

    [Fact]
    public void Unparseable_input_is_reported_rather_than_thrown()
    {
        Assert.False(MimeSubmission.TryPrepare([0x00, 0xff, 0xfe], Domain, out _));
    }
}
