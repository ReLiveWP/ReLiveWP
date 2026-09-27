using System.Collections.Frozen;
using System.Text.Json;

namespace ReLiveWP.Services.Devices.Services;

public class CarrierLookupService(ILogger<CarrierLookupService> logger) : IHostedService, ICarrierLookupService
{
    private FrozenDictionary<string, CarrierInfo>? CarrierLookup { get; set; }

    public async Task LoadAsync()
    {
        var dictionary = new Dictionary<string, CarrierInfo>();

        await using var stream = File.OpenRead("wwwroot/carriers.json");
        await foreach (var carrier in JsonSerializer.DeserializeAsyncEnumerable<CarrierInfo>(stream))
        {
            var key = carrier.MCC + carrier.MNC;
            dictionary[key] = carrier;
        }

        CarrierLookup = dictionary.ToFrozenDictionary();
    }

    public CarrierInfo? GetCarrierInfo(string carrierId)
        => CarrierLookup?.TryGetValue(carrierId, out var info) == true ? info : null;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await LoadAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
