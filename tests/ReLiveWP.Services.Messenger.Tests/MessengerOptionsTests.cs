using ReLiveWP.Services.Messenger.Data;

namespace ReLiveWP.Services.Messenger.Tests;

public class MessengerOptionsTests
{
    private readonly MessengerOptions options = new() { MaxSessionTimeout = TimeSpan.FromDays(3) };

    [Fact]
    public void HonoursAShorterRequestedTimeout()
    {
        Assert.Equal(TimeSpan.FromHours(1), options.ResolveSessionTtl(3600));
    }

    [Fact]
    public void CapsALongerRequestedTimeout()
    {
        Assert.Equal(TimeSpan.FromDays(3), options.ResolveSessionTtl((int)TimeSpan.FromDays(30).TotalSeconds));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void FallsBackToTheMaximumWithoutAUsableRequest(int? requestedSeconds)
    {
        Assert.Equal(TimeSpan.FromDays(3), options.ResolveSessionTtl(requestedSeconds));
    }

    [Fact]
    public void ASessionThatHasNotSignedInGetsTheShortTimeout()
    {
        Assert.Equal(options.PreAuthSessionTimeout, options.ResolveSessionTtl(MsnpGatewaySessionState.AwaitingSsoTicket, 259200));
        Assert.Equal(TimeSpan.FromDays(3), options.ResolveSessionTtl(MsnpGatewaySessionState.Authenticated, 259200));
    }
}
