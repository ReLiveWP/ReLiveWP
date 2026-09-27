using System.Diagnostics.Metrics;
using ReLiveWP.ServiceDefaults;

namespace ReLiveWP.Backend.Identity;

public static class IdentityMetrics
{
    private static readonly Counter<long> AuthAttempts = ServiceTelemetry.Meter.CreateCounter<long>(
        "relivewp.identity.auth_attempts", "{attempt}");

    private static readonly Counter<long> RefreshTokenRedemptions = ServiceTelemetry.Meter.CreateCounter<long>(
        "relivewp.identity.refresh_token.redemptions", "{attempt}");

    public static void RecordAuthAttempt(string method, string outcome) =>
        AuthAttempts.Add(1,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("outcome", outcome));

    public static void RecordRefreshTokenRedemption(string outcome) =>
        RefreshTokenRedemptions.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}
