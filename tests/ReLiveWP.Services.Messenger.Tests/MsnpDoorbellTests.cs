using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Services.Messenger.Data;
using ReLiveWP.Services.Messenger.Services;

namespace ReLiveWP.Services.Messenger.Tests;

public class MsnpDoorbellTests
{
    private static readonly IReadOnlySet<string> PushHosts = new MessengerOptions().PushHosts;

    [Fact]
    public void OurPushChannelIsRewrittenToTheInternalAddress()
    {
        var url = MsnpDoorbell.ResolvePushUrl(
            "https://push.int.relivewp.net/channel/4dc402302ebc4e77b7670f4429bae245", PushHosts, "http://push:10005");

        Assert.Equal("http://push:10005/channel/4dc402302ebc4e77b7670f4429bae245", url?.ToString());
    }

    [Fact]
    public void WithoutAnInternalAddressThePublicUrlIsUsed()
    {
        var url = MsnpDoorbell.ResolvePushUrl("https://push.relivewp.net/channel/abc", PushHosts, null);

        Assert.Equal("https://push.relivewp.net/channel/abc", url?.ToString());
    }

    [Theory]
    [InlineData("https://attacker.example/channel/abc")]
    [InlineData("http://push.relivewp.net/channel/abc")]
    [InlineData("https://push.relivewp.net.attacker.example/channel/abc")]
    [InlineData("not a url")]
    [InlineData("")]
    public void AnythingElseIsNotRung(string notificationUri)
    {
        Assert.Null(MsnpDoorbell.ResolvePushUrl(notificationUri, PushHosts, "http://push:10005"));
    }

    [Fact]
    public async Task EveryRememberedDeviceIsRungOnceWithinTheWindow()
    {
        var devices = new InMemoryDeviceStore();
        await devices.RememberDeviceAsync("user-bob", new MsnpDevice("phone", "https://push.relivewp.net/channel/phone", DateTimeOffset.UtcNow));
        await devices.RememberDeviceAsync("user-bob", new MsnpDevice("tablet", "https://push.relivewp.net/channel/tablet", DateTimeOffset.UtcNow));
        await devices.RememberDeviceAsync("user-bob", new MsnpDevice("odd", "https://attacker.example/channel/odd", DateTimeOffset.UtcNow));
        var push = new RecordingHandler();
        var doorbell = CreateDoorbell(devices, push);

        await doorbell.RingDevicesAsync("user-bob", CancellationToken.None);
        await doorbell.RingDevicesAsync("user-bob", CancellationToken.None);

        Assert.Equal(["http://push:10005/channel/phone", "http://push:10005/channel/tablet"], push.Posted.Order());
    }

    [Fact]
    public async Task ADeviceWhosePushTimesOutDoesNotStopTheRest()
    {
        var devices = new InMemoryDeviceStore();
        await devices.RememberDeviceAsync("user-bob", new MsnpDevice("phone", "https://push.relivewp.net/channel/phone", DateTimeOffset.UtcNow));
        await devices.RememberDeviceAsync("user-bob", new MsnpDevice("tablet", "https://push.relivewp.net/channel/tablet", DateTimeOffset.UtcNow));
        var push = new RecordingHandler { TimingOut = { "http://push:10005/channel/phone" } };
        var doorbell = CreateDoorbell(devices, push);

        await doorbell.RingDevicesAsync("user-bob", CancellationToken.None);

        Assert.Equal(["http://push:10005/channel/phone", "http://push:10005/channel/tablet"], push.Posted.Order());
    }

    private static MsnpDoorbell CreateDoorbell(InMemoryDeviceStore devices, RecordingHandler push)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Endpoints:Push"] = "http://push:10005" })
            .Build();

        return new MsnpDoorbell(
            new InMemorySessionStore(), devices, new SingleClientFactory(push), Options.Create(new MessengerOptions()),
            configuration, TimeProvider.System, NullLogger<MsnpDoorbell>.Instance);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Posted { get; } = [];

        public HashSet<string> TimingOut { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            Posted.Add(url);
            if (TimingOut.Contains(url))
                throw new TimeoutException("the resilience handler gave up");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
