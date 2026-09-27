using System.Security.Claims;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using ReLiveWP.Identity;
using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Activity.Models.Web;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Activity.Utilities;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Activity.Endpoints;

public static class SocialEndpoints
{
    private const int MaxFeedCount = 50;
    private const int MaxReplyCount = 49;
    private const int MaxReplyLength = 300;
    private static readonly TimeSpan WebFeedLifetime = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan WebRepliesLifetime = TimeSpan.FromSeconds(30);

    public static void MapSocialEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/social").RequireAuthorization();

        group.MapGet("/providers", GetProviders);
        group.MapGet("/feed", GetFeedAsync);
        group.MapGet("/contacts/{cid}/feed", GetContactFeedAsync);
        group.MapGet("/replies", GetRepliesAsync);
        group.MapPost("/replies", PostReplyAsync);
        group.MapGet("/contacts/{serverId}/identities", GetIdentitiesAsync);
        group.MapPost("/contacts/{serverId}/identities", BindIdentityAsync);
        group.MapDelete("/contacts/{serverId}/identities/{provider}", UnbindIdentityAsync);
    }

    private static IResult GetProviders(ActivityProviderService providers)
    {
        var listed = providers.PublicProviders.Select(p => new SocialProvider(p.IdentityProvider, p.Name));
        return Results.Ok(new SocialProvidersResponse([.. listed]));
    }

    private static async Task<IResult> GetIdentitiesAsync(
        string serverId,
        ClaimsPrincipal user,
        ActivityProviderService providers,
        MailboxStore.MailboxStoreClient mailbox,
        MediaProxyUrlSigner mediaProxy,
        CancellationToken ct)
    {
        var userId = user.Id()!;

        ResolveContactIdentitiesResponse listed;
        try
        {
            listed = await mailbox.ListContactIdentitiesAsync(
                new ListContactIdentitiesRequest { UserId = userId, ServerId = serverId }, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return Results.NotFound(new SocialErrorResponse("contact_not_found"));
        }

        var identities = await Task.WhenAll(
            listed.Identities.Select(row => DescribeAsync(providers, mediaProxy, row.Provider, row.ExternalId, ct)));

        var cid = listed.Identities.Count > 0 ? listed.Identities[0].ContactCid : Cids.ContactCid(userId, serverId);
        return Results.Ok(new SocialIdentitiesResponse(WebCids.FormatCid(cid), identities));
    }

    private static async Task<IResult> BindIdentityAsync(
        string serverId,
        SocialBindRequest request,
        ClaimsPrincipal user,
        ActivityProviderService providers,
        MailboxStore.MailboxStoreClient mailbox,
        MediaProxyUrlSigner mediaProxy,
        CancellationToken ct)
    {
        var provider = providers.FindPublicProvider(request.Provider);
        if (provider == null)
            return Results.BadRequest(new SocialErrorResponse("unknown_provider"));

        if (string.IsNullOrWhiteSpace(request.Handle))
            return Results.BadRequest(new SocialErrorResponse("invalid_handle"));

        var resolved = await provider.ResolveIdentityAsync(request.Handle, ct);
        if (resolved == null)
            return Results.NotFound(new SocialErrorResponse("handle_not_found"));

        var userId = user.Id()!;

        ContactIdentity bound;
        try
        {
            bound = await mailbox.BindContactIdentityAsync(new BindContactIdentityRequest
            {
                UserId = userId,
                ServerId = serverId,
                Provider = resolved.Provider,
                ExternalId = resolved.ExternalId,
                ContactCid = Cids.ContactCid(userId, serverId),
            }, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return Results.NotFound(new SocialErrorResponse("contact_not_found"));
        }

        var identity = SocialIdentity.From(resolved, mediaProxy);
        return Results.Ok(new SocialIdentitiesResponse(WebCids.FormatCid(bound.ContactCid), [identity]));
    }

    private static async Task<IResult> UnbindIdentityAsync(
        string serverId,
        string provider,
        ClaimsPrincipal user,
        MailboxStore.MailboxStoreClient mailbox,
        CancellationToken ct)
    {
        try
        {
            var result = await mailbox.UnbindContactIdentityAsync(
                new UnbindContactIdentityRequest { UserId = user.Id()!, ServerId = serverId, Provider = provider },
                cancellationToken: ct);

            return result.Found ? Results.NoContent() : Results.NotFound(new SocialErrorResponse("identity_not_found"));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return Results.NotFound(new SocialErrorResponse("contact_not_found"));
        }
    }

    // a stored identity is just a provider and an id, the name and picture are looked up each time
    // and the provider caches them
    private static async Task<SocialIdentity> DescribeAsync(
        ActivityProviderService providers, MediaProxyUrlSigner mediaProxy, string providerToken, string externalId,
        CancellationToken ct)
    {
        var provider = providers.FindPublicProvider(providerToken);
        var resolved = provider == null ? null : await provider.ResolveIdentityAsync(externalId, ct);
        return resolved == null
            ? new SocialIdentity(providerToken, externalId, "", externalId, "")
            : SocialIdentity.From(resolved, mediaProxy);
    }

    private static async Task<IResult> GetFeedAsync(
        ClaimsPrincipal user,
        ActivityProviderService providers,
        ActivityFeedReader reader,
        IMemoryCache cache,
        MediaProxyUrlSigner mediaProxy,
        int count = 20)
    {
        var userId = user.Id()!;
        count = Math.Clamp(count, 1, MaxFeedCount);

        var key = $"web:feed:{userId}";
        if (cache.TryGetValue<SocialFeedResponse>(key, out var cached) && cached != null && cached.Entries.Count >= count)
            return Results.Ok(cached with { Entries = [.. cached.Entries.Take(count)] });

        var provider = await providers.GetOwnedProviderAsync(OwnedProviderUse.Read);
        if (provider is not { HasSources: true })
            return Results.Ok(new SocialFeedResponse(false, []));

        var entries = await reader.ReadOwnFeedAsync(provider, ActivitiesContext.Contacts, count, userId);
        var response = new SocialFeedResponse(true, [.. entries.Select(entry => RenderEntry(entry, mediaProxy))]);
        cache.Set(key, response, WebFeedLifetime);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetContactFeedAsync(
        string cid,
        ClaimsPrincipal user,
        ActivityProviderService providers,
        ActivityFeedReader reader,
        MediaProxyUrlSigner mediaProxy,
        CancellationToken ct,
        int count = 20)
    {
        if (!WebCids.TryParseCid(cid, out var contactCid))
            return Results.BadRequest(new SocialErrorResponse("invalid_cid"));

        count = Math.Clamp(count, 1, MaxFeedCount);

        var sources = await providers.GetContactFeedSourcesAsync(contactCid, user.Id()!, ct);
        var entries = await reader.ReadContactFeedAsync(providers.PublicProviders, sources, contactCid, count);

        var rendered = entries.Select(entry => RenderEntry(entry, mediaProxy));
        return Results.Ok(new SocialContactFeedResponse(WebCids.FormatCid(contactCid), sources.Count > 0, [.. rendered]));
    }

    private static async Task<IResult> GetRepliesAsync(
        string activityId,
        ClaimsPrincipal user,
        ActivityProviderService providers,
        ActivityFeedReader reader,
        IMemoryCache cache,
        MediaProxyUrlSigner mediaProxy,
        int count = 20)
    {
        if (!ActivityIds.TrySplit(activityId, out var providerId, out var id))
            return Results.BadRequest(new SocialErrorResponse("invalid_activity_id"));

        var userId = user.Id()!;
        count = Math.Clamp(count, 1, MaxReplyCount);

        var key = GetReplyCacheKey(userId, activityId);
        if (cache.TryGetValue<SocialRepliesResponse>(key, out var cached) && cached != null && cached.Entries.Count >= count)
            return Results.Ok(cached with { Entries = [.. cached.Entries.Take(count)] });

        var replyProviders = await providers.GetReplyProvidersAsync();
        var entries = await reader.ReadRepliesAsync(replyProviders, providerId, id, count, userId);

        var response = new SocialRepliesResponse(activityId, [.. entries.Select(entry => RenderEntry(entry, mediaProxy))]);
        cache.Set(key, response, WebRepliesLifetime);
        return Results.Ok(response);
    }

    private static async Task<IResult> PostReplyAsync(
        SocialReplyRequest request,
        ClaimsPrincipal user,
        ActivityProviderService providers,
        IMemoryCache cache)
    {
        if (!ActivityIds.TrySplit(request.ActivityId, out var providerId, out var id))
            return Results.BadRequest(new SocialErrorResponse("invalid_activity_id"));

        var text = request.Text?.Trim() ?? "";
        if (text.Length is 0 or > MaxReplyLength)
            return Results.BadRequest(new SocialErrorResponse("invalid_text"));

        var provider = await providers.GetOwnedProviderAsync(OwnedProviderUse.Post);
        if (provider is not { HasSources: true })
            return Results.Conflict(new SocialErrorResponse("not_connected"));

        var posted = await provider.CreateReplyAsync(providerId, id, text);
        if (!posted)
            return Results.Json(new SocialErrorResponse("post_failed"), statusCode: StatusCodes.Status502BadGateway);

        cache.Remove(GetReplyCacheKey(user.Id()!, request.ActivityId));
        return Results.Ok(new SocialReplyResponse(true));
    }

    private static string GetReplyCacheKey(string userId, string activityId) => $"web:replies:{userId}:{activityId}";

    private static SocialEntry RenderEntry(ResolvedEntry resolved, MediaProxyUrlSigner mediaProxy)
        => SocialEntry.From(resolved.Entry, WebCids.FormatCid(resolved.AuthorCid), mediaProxy);
}
