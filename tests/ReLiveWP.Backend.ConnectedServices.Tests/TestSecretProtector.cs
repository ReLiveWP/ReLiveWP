using Microsoft.Extensions.Configuration;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Tests;

public static class TestSecretProtector
{
    public static ConnectionSecretProtector CreateKeyed()
    {
        var key = Convert.ToHexString(new byte[32]);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ConnectionSecretProtector.KeyConfigPath] = key })
            .Build();

        return new ConnectionSecretProtector(configuration);
    }
}
