using System.Globalization;
using ReLiveWP.ServiceDefaults.Contacts;

namespace ReLiveWP.Services.Activity.Utilities;

public enum ActivitySubjectKind
{
    Owner,
    Contact,
    Unknown,
}

public readonly record struct ActivitySubject(ActivitySubjectKind Kind, long Cid, string StoreSourceId);

public static class ActivitySubjects
{
    public const string LiveSourceId = "WL";

    public static ActivitySubject Resolve(string? sourceId, string? objectId, long ownerCid)
    {
        var isAggregate = string.Equals(sourceId, AggregateNetwork.DomainTag, StringComparison.OrdinalIgnoreCase);
        var storeSourceId = isAggregate ? AggregateNetwork.DomainTag : LiveSourceId;

        var owner = new ActivitySubject(ActivitySubjectKind.Owner, ownerCid, storeSourceId);
        var unknown = new ActivitySubject(ActivitySubjectKind.Unknown, 0, storeSourceId);

        if (objectId == null)
            return owner;

        if (string.IsNullOrEmpty(sourceId) || string.Equals(sourceId, LiveSourceId, StringComparison.OrdinalIgnoreCase))
        {
            if (!long.TryParse(objectId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cid))
                return unknown;

            return cid == ownerCid ? owner : new ActivitySubject(ActivitySubjectKind.Contact, cid, storeSourceId);
        }

        if (isAggregate && string.Equals(objectId, AggregateNetwork.DomainTag, StringComparison.OrdinalIgnoreCase))
            return owner;

        return unknown;
    }
}
