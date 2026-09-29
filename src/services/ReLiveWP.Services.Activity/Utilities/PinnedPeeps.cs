using System.Globalization;

namespace ReLiveWP.Services.Activity.Utilities;

public readonly record struct PinnedPeep(string SourceId, string ObjectId);

// $xslt_peeps on a wp7ctsm request: ",SourceId:ObjectId,SourceId:ObjectId," with one pair per pinned contact
public static class PinnedPeeps
{
    public static List<PinnedPeep> ParsePeeps(string? peeps)
    {
        if (string.IsNullOrEmpty(peeps))
            return [];

        var parsed = new List<PinnedPeep>();
        foreach (var pair in peeps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = pair.IndexOf(':');
            if (separator <= 0 || separator == pair.Length - 1)
                continue;

            parsed.Add(new PinnedPeep(pair[..separator], pair[(separator + 1)..]));
        }

        return parsed;
    }

    public static List<long> SelectLiveCids(IEnumerable<PinnedPeep> peeps)
    {
        var cids = new List<long>();
        foreach (var peep in peeps)
        {
            if (!string.Equals(peep.SourceId, ActivitySubjects.LiveSourceId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!long.TryParse(peep.ObjectId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cid))
                continue;

            if (!cids.Contains(cid))
                cids.Add(cid);
        }

        return cids;
    }
}
