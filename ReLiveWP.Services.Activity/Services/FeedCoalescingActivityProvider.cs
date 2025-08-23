using System.Diagnostics.CodeAnalysis;
using ReLiveWP.Services.Activity.Models;

namespace ReLiveWP.Services.Activity.Services;

public class FeedCoalescingActivityProvider(IReadOnlyList<ActivityProviderBase> providers) : ActivityProviderBase
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
            return obj.GetHashCode();
        }
    }

    public override string Name => "All Feeds";
    public override string ProviderId => "ALL";

    public override async IAsyncEnumerable<EntryModel> GetEntriesAsync(ActivitiesContext context, int count)
    {
        if (providers.Count == 0)
        {
            yield return new EntryModel()
            {
                Id = "NotARealPost",
                Author = new ProfileModel()
                {
                    Id = "NotARealUser",
                    DisplayName = "ReLive System",
                    AvatarUrl = "",
                    CanonicalUrl = "",
                    IsMe = context == ActivitiesContext.My
                },
                ProviderId = this.ProviderId,
                EntryType = EntryType.Article,
                Title = "Nothing to see here!",
                Content = "You've not linked any accounts yet! If you want to see your social feeds here, visit http://link.relivewp.net to get started!",
                Published = new DateTime(2025, 08, 01),
                Categories = ["post"],
                Generator = $"ReLive System",
                CanonicalUrl = $"",
                CanReply = false,
                ReplyCount = 0
            };

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
}
