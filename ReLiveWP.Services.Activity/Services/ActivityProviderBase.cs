using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Services;

public enum ActivitiesContext
{
    My, Contacts, Media
}

public abstract class ActivityProviderBase
{
    public abstract string Name { get; }
    public abstract string ProviderId { get; }
    public abstract Task CreatePostAsync(string text);
    public abstract IAsyncEnumerable<EntryModel> GetEntriesAsync(ActivitiesContext context, int count);
}
