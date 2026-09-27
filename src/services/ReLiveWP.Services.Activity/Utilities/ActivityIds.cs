namespace ReLiveWP.Services.Activity.Utilities;

public static class ActivityIds
{
    public static bool TrySplit(string? activityId, out string providerId, out string id)
    {
        providerId = "";
        id = "";
        if (string.IsNullOrEmpty(activityId))
            return false;

        var colon = activityId.IndexOf(':');
        if (colon <= 0 || colon == activityId.Length - 1)
            return false;

        providerId = activityId[..colon];
        id = activityId[(colon + 1)..];
        return true;
    }
}
