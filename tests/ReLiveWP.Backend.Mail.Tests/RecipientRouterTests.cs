using Microsoft.Extensions.Options;
using ReLiveWP.Backend.Mail.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Mail.Tests;

public class RecipientRouterTests
{
    private static RecipientRouter NewRouter(FakeUserClient users) =>
        new(users, Options.Create(new MailOptions { LocalDomains = ["relivewp.net"] }));

    private static LookupUsersByEmailResponse Found(params (string Email, string UserId)[] users)
    {
        var response = new LookupUsersByEmailResponse();
        foreach (var (email, userId) in users)
            response.Users.Add(new DirectoryUser { QueriedEmail = email, UserId = userId });
        return response;
    }

    [Fact]
    public async Task A_local_mailbox_routes_local_with_its_user_id()
    {
        var users = new FakeUserClient
        {
            OnLookupUsersByEmail = _ => Found(("ada@relivewp.net", "user-ada")),
        };

        var resolved = await NewRouter(users).ResolveAsync(["ada@relivewp.net"], default);

        var recipient = Assert.Single(resolved);
        Assert.Equal(MailRoute.Local, recipient.Route);
        Assert.Equal("user-ada", recipient.UserId);
    }

    [Fact]
    public async Task A_local_domain_with_no_mailbox_is_unroutable()
    {
        var users = new FakeUserClient { OnLookupUsersByEmail = _ => Found() };

        var resolved = await NewRouter(users).ResolveAsync(["nobody@relivewp.net"], default);

        Assert.Equal(MailRoute.Unroutable, Assert.Single(resolved).Route);
    }

    [Fact]
    public async Task An_outside_domain_routes_external()
    {
        var users = new FakeUserClient();

        var resolved = await NewRouter(users).ResolveAsync(["someone@gmail.com"], default);

        Assert.Equal(MailRoute.External, Assert.Single(resolved).Route);
        Assert.Null(users.LastLookup);
    }

    [Fact]
    public async Task Private_profiles_are_included_in_the_lookup()
    {
        var users = new FakeUserClient { OnLookupUsersByEmail = _ => Found() };

        await NewRouter(users).ResolveAsync(["ada@relivewp.net"], default);

        // a private profile still has a mailbox and still has to receive mail
        Assert.True(users.LastLookup!.IncludePrivate);
    }

    [Fact]
    public async Task Mixed_recipients_keep_their_input_order()
    {
        var users = new FakeUserClient
        {
            OnLookupUsersByEmail = _ => Found(("ada@relivewp.net", "user-ada")),
        };

        var resolved = await NewRouter(users).ResolveAsync(
            ["someone@gmail.com", "ada@relivewp.net", "nobody@relivewp.net"], default);

        Assert.Equal(
            [MailRoute.External, MailRoute.Local, MailRoute.Unroutable],
            resolved.Select(r => r.Route));
    }

    [Fact]
    public async Task Domain_matching_ignores_case()
    {
        var users = new FakeUserClient
        {
            OnLookupUsersByEmail = _ => Found(("ada@ReLiveWP.NET", "user-ada")),
        };

        var resolved = await NewRouter(users).ResolveAsync(["ada@ReLiveWP.NET"], default);

        Assert.Equal(MailRoute.Local, Assert.Single(resolved).Route);
    }
}
