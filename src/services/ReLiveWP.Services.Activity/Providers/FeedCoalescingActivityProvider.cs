using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Providers;

public class FeedCoalescingActivityProvider(IReadOnlyList<OwnedActivityProviderBase> providers, ILogger logger) : OwnedActivityProviderBase
{
    private class EntryEqualityComparaer : IEqualityComparer<EntryModel>
    {
        public static EntryEqualityComparaer Instance { get; } = new EntryEqualityComparaer();

        public bool Equals(EntryModel? x, EntryModel? y)
        {
            if (x == null && y == null)
                return true;

            if (x == null && y != null || x != null && y == null)
                return false;

            // TODO: this will, eventually, turn into a pretty long process of deduplication involving connected authors and whatever
            //       but for now we're just gonna try to catch the exact copy-paste situations
            return string.Compare(x!.Content, y!.Content, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public int GetHashCode(EntryModel obj)
        {
            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.Content);
        }
    }

    private static readonly EntryModel DefaultEntry = new EntryModel()
    {
        Id = "NotARealPost",
        Author = new ProfileModel()
        {
            Id = "NotARealUser",
            DisplayName = "ReLive System",
            ScreenName = "@relive.system",
            AvatarUrl = "",
            CanonicalUrl = "",
            IsMe = true,
        },
        ProviderId = "ALL",
        EntryType = EntryType.Article,
        Title = "Nothing to see here!",
        Content = "You've not linked any accounts yet! If you want to see your social feeds here, visit https://link.relivewp.net to get started!",
        Published = new DateTime(2025, 08, 01),
        Categories = ["post"],
        Generator = $"ReLive System",
        CanonicalUrl = $"",
        CanReply = false,
        ReplyCount = 0
    };

    public override string Name => "All Feeds";
    public override string ProviderId => "ALL";
    public override bool HasSources => providers.Count > 0;

    public override async IAsyncEnumerable<EntryModel> GetEntriesAsync(ActivitiesContext context, int count)
    {
        if (providers.Count == 0)
        {
            yield return DefaultEntry;

            yield break;
        }

        await foreach (var item in providers.ToAsyncEnumerable()
                        .SelectMany(s => s.GetEntriesAsync(context, (int)Math.Ceiling(((double)count / providers.Count) * 1.5)))
                        .Distinct(EntryEqualityComparaer.Instance)
                        .OrderByDescending(d => d.Published)
                        .Take(count))
        {
            yield return item;
        }
    }

    public override async IAsyncEnumerable<EntryModel> GetRepliesAsync(string providerId, string activityId, int count)
    {
        // providers are expected to ignore IDs they don't understand
        HashSet<string> seen = [];
        foreach (var provider in providers)
        {
            await foreach (var item in provider.GetRepliesAsync(providerId, activityId, count))
            {
                if (seen.Add(GetReplyKey(item)))
                    yield return item;
            }
        }
    }

    public override async Task CreatePostAsync(string text)
    {
        var attempts = providers.Select(provider => TryCreatePostAsync(provider, text));
        var results = await Task.WhenAll(attempts);

        var failures = results.OfType<Exception>().ToList();
        if (failures.Count > 0 && failures.Count == results.Length)
            throw new AggregateException(failures);
    }

    public override async Task<bool> CreateReplyAsync(string providerId, string activityId, string text)
    {
        foreach (var provider in providers)
        {
            if (await provider.CreateReplyAsync(providerId, activityId, text))
                return true;
        }

        return false;
    }

    // the same status read through two fediverse accounts gets a different id per instance, the url stays put
    private static string GetReplyKey(EntryModel reply)
        => string.IsNullOrEmpty(reply.CanonicalUrl) ? $"{reply.ProviderId}:{reply.Id}" : reply.CanonicalUrl;

    private async Task<Exception?> TryCreatePostAsync(OwnedActivityProviderBase provider, string text)
    {
        try
        {
            await provider.CreatePostAsync(text);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Posting to {Provider} failed", provider.Name);
            return ex;
        }
    }
}
