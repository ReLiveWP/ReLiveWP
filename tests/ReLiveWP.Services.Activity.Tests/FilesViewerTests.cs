using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Tests;

public class FilesViewerTests
{
    private const string UserId = "user-1";
    private const long OwnerCid = 0x000e5a1b2c3d4e5fL;
    private static readonly string OwnerCidHex = OwnerCid.ToString("X16");
    private static readonly string OwnerRoute = OwnerCid.ToString();

    private static ClaimsPrincipal Principal(string? cid)
    {
        List<Claim> claims = [new Claim(ClaimTypes.NameIdentifier, UserId)];
        if (cid != null)
            claims.Add(new Claim("cid", cid));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static FileViewerService Viewer(FakeUserClient users)
        => new(users, TestCache.New(), NullLogger<FileViewerService>.Instance);

    [Fact]
    public async Task TheCidClaimAnswersWithoutAskingIdentity()
    {
        var users = new FakeUserClient();
        var viewer = Viewer(users);

        Assert.Null(await viewer.SubjectCidAsync(OwnerRoute, Principal(OwnerCidHex)));
        Assert.Equal(42, await viewer.SubjectCidAsync("42", Principal(OwnerCidHex)));
        Assert.Equal(0, users.GetUserInfoCalls);
    }

    [Fact]
    public async Task WithoutTheClaimIdentityIsAskedOnce()
    {
        var users = new FakeUserClient { OnGetUserInfo = _ => new GetUserInfoResponse { Cid = OwnerCidHex } };
        var viewer = Viewer(users);

        Assert.Null(await viewer.SubjectCidAsync(OwnerRoute, Principal(null)));
        Assert.Equal(42, await viewer.SubjectCidAsync("42", Principal(null)));
        Assert.Equal(1, users.GetUserInfoCalls);
    }

    [Fact]
    public async Task ARouteThatIsNotACidIsSelf()
    {
        var users = new FakeUserClient();

        Assert.Null(await Viewer(users).SubjectCidAsync("wmphotos", Principal(null)));
        Assert.Equal(0, users.GetUserInfoCalls);
    }

    [Fact]
    public async Task AFailedLookupIsSelf()
    {
        var users = new FakeUserClient { OnGetUserInfo = _ => throw new InvalidOperationException("down") };

        Assert.Null(await Viewer(users).SubjectCidAsync("42", Principal(null)));
    }
}
