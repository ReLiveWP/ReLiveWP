using ReLiveWP.Services.Activity.Utilities;

namespace ReLiveWP.Services.Activity.Tests;

public class WebCidsTests
{
    [Theory]
    [InlineData("15fe5d7a6d8d65ff")]
    [InlineData("9c682049f35ff806")]
    [InlineData("0000000000000001")]
    public void A_cid_round_trips(string text)
    {
        Assert.True(WebCids.TryParseCid(text, out var cid));
        Assert.Equal(text, WebCids.FormatCid(cid));
    }

    [Fact]
    public void A_top_bit_cid_parses_to_the_signed_value_the_mailbox_stores()
    {
        Assert.True(WebCids.TryParseCid("9c682049f35ff806", out var cid));
        Assert.Equal(unchecked((long)0x9c682049f35ff806), cid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("19c682049f35ff806")]
    [InlineData("not-a-cid")]
    public void A_malformed_cid_is_rejected(string text)
    {
        Assert.False(WebCids.TryParseCid(text, out _));
    }
}
