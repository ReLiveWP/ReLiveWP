using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Providers;

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

public record ResolvedIdentity(string Provider, string ExternalId, string Handle, string DisplayName, string AvatarUrl);

public abstract class PublicActivityProviderBase : ActivityProviderBase
{
    public abstract IAsyncEnumerable<EntryModel> GetAuthorEntriesAsync(string provider, string externalId, int count);

    // takes a handle or a provider-native id, answers null when the provider has nobody by that name
    public abstract Task<ResolvedIdentity?> ResolveIdentityAsync(string handleOrId, CancellationToken ct = default);
}

public abstract class OwnedActivityProviderBase : ActivityProviderBase
{
    public virtual bool HasSources => true;

    public abstract Task CreatePostAsync(string text);
    public abstract Task<bool> CreateReplyAsync(string provider, string activityId, string text);
    public abstract IAsyncEnumerable<EntryModel> GetEntriesAsync(ActivitiesContext context, int count);
}
