using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Services;

public abstract class ActivityProviderBase
{
    public abstract string Name { get; }
    public abstract string ProviderId { get; }
    public abstract IAsyncEnumerable<EntryModel> GetEntriesAsync(ActivitiesContext context, int count);
}
