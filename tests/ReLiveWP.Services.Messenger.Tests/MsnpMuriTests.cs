using ReLiveWP.Services.Messenger.Msnp;
using static ReLiveWP.Services.Messenger.Tests.MsnpSamples;

namespace ReLiveWP.Services.Messenger.Tests;

public class MsnpMuriTests
{
    [Theory]
    [InlineData("1:alice@example.com;epid=" + Epid)]
    [InlineData("1:alice@example.com;epid=11111111-2222-3333-4444-555555555555")]
    [InlineData("1:alice@example.com; EPID = {11111111-2222-3333-4444-555555555555}")]
    public void ReadsTheEpidAsAGuid(string value)
    {
        Assert.True(MsnpMuri.TryParse(value, out var muri));

        Assert.Equal(MsnpMuri.WindowsLiveType, muri.Type);
        Assert.Equal("alice@example.com", muri.Address);
        Assert.Equal(Guid.Parse(Epid), muri.Epid);
    }

    [Theory]
    [InlineData("1:alice@example.com;epid={\" x=\"}")]
    [InlineData("1:alice@example.com;epid=<evil/>")]
    [InlineData("1:alice@example.com;epid=")]
    [InlineData("1:alice@example.com;epid=(11111111-2222-3333-4444-555555555555)")]
    public void RefusesAnEpidThatIsNotABracedOrPlainGuid(string value)
    {
        Assert.False(MsnpMuri.TryParse(value, out _));
    }

    [Fact]
    public void AMuriWithoutAnEpidHasNone()
    {
        Assert.True(MsnpMuri.TryParse("1:bob@example.com", out var muri));

        Assert.Null(muri.Epid);
        Assert.Equal("1:bob@example.com", muri.ToString());
    }

    [Fact]
    public void WritesTheEpidBracedAndUpperCase()
    {
        var muri = MsnpMuri.ForWindowsLive("alice@example.com", Guid.Parse("aaaaaaaa-2222-3333-4444-555555555555"));

        Assert.Equal("1:alice@example.com;epid={AAAAAAAA-2222-3333-4444-555555555555}", muri.ToString());
    }
}
