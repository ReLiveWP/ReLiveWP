using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReLiveWP.Backend.ClearingHouse.Services.Mirror;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

public class MirrorPollerRegistrationTests
{
    // AddHostedService goes through TryAddEnumerable, which dedupes on service type plus
    // implementation type. Both kinds are (IHostedService, MirrorPoller), so registering them that
    // way silently keeps one and the other kind queues forever with nothing sweeping it.
    [Fact]
    public void Every_kind_gets_its_own_hosted_service()
    {
        var services = new ServiceCollection();

        services.AddMirrorPoller(MirrorKind.Contacts);
        services.AddMirrorPoller(MirrorKind.Calendar);

        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(IHostedService)));
    }
}
