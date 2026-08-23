using ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

namespace ReLiveWP.Backend.ClearingHouse.Tests;

// Google signals a dead sync token with 400 FAILED_PRECONDITION, not the 410 that Graph uses. get
// this wrong and the driver never throws DeltaTokenExpiredException, so the full-sync fallback in
// MirrorRunner never runs and the source fails forever on the same stored token.
public class GooglePeopleErrorTests
{
    private const string Expired = """
        {
          "error": {
            "code": 400,
            "message": "Sync token is expired. Clear local cache and retry call without the sync token.",
            "status": "FAILED_PRECONDITION",
            "details": [
              {
                "@type": "type.googleapis.com/google.rpc.ErrorInfo",
                "reason": "EXPIRED_SYNC_TOKEN",
                "domain": "people.googleapis.com"
              }
            ]
          }
        }
        """;

    private const string Corrupt = """
        {
          "error": {
            "code": 400,
            "message": "Request contains an invalid argument.",
            "status": "INVALID_ARGUMENT"
          }
        }
        """;

    private const string Quota = """
        {
          "error": {
            "code": 429,
            "message": "Quota exceeded for quota metric 'Critical read requests'.",
            "status": "RESOURCE_EXHAUSTED"
          }
        }
        """;

    [Fact]
    public void An_expired_sync_token_is_rejected()
        => Assert.True(GooglePeopleErrors.IsSyncTokenRejected(400, Expired));

    [Fact]
    public void A_corrupt_sync_token_is_rejected()
        => Assert.True(GooglePeopleErrors.IsSyncTokenRejected(400, Corrupt));

    [Fact]
    public void A_quota_failure_is_not_a_token_problem()
        => Assert.False(GooglePeopleErrors.IsSyncTokenRejected(429, Quota));

    [Fact]
    public void Another_400_is_not_a_token_problem()
        => Assert.False(GooglePeopleErrors.IsSyncTokenRejected(400, """
            { "error": { "code": 400, "status": "PERMISSION_DENIED" } }
            """));

    [Fact]
    public void A_success_is_never_a_token_problem()
        => Assert.False(GooglePeopleErrors.IsSyncTokenRejected(200, """{ "connections": [] }"""));

    [Theory]
    [InlineData("")]
    [InlineData("<html>502 Bad Gateway</html>")]
    [InlineData("{ \"error\": \"nope\" }")]
    [InlineData("{ \"error\": { \"status\": 400 } }")]
    public void An_unreadable_body_is_not_a_token_problem(string json)
        => Assert.False(GooglePeopleErrors.IsSyncTokenRejected(400, json));
}
