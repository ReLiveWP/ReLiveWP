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
    public virtual string IdentityProvider => ProviderId;

    public abstract IAsyncEnumerable<EntryModel> GetRepliesAsync(string provider, string activityId, int count);
}

public abstract class PublicActivityProviderBase : ActivityProviderBase
{
    public abstract IAsyncEnumerable<EntryModel> GetAuthorEntriesAsync(string provider, string externalId, int count);
}

public abstract class OwnedActivityProviderBase : ActivityProviderBase
{
    public abstract Task CreatePostAsync(string text);
    public abstract Task<bool> CreateReplyAsync(string provider, string activityId, string text);
    public abstract IAsyncEnumerable<EntryModel> GetEntriesAsync(ActivitiesContext context, int count);
}
