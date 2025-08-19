using System.Net.Http.Headers;
using Duende.IdentityModel.OidcClient.DPoP;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Identity.ConnectedServices;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Backend.Identity.Services;

namespace ReLiveWP.Backend.Identity;

// 
// i know what you're thinking. wam, what the fuck is this?
//
// DPoP requires keys to be in the posession of the requester at all times. this is frustrating if i'm fetching from a different
// service to the one that handles the keys. this is an auth proxy. it sits between me and your PDS and replaces my authentication scheme
// with standard bearer tokens, with the stored access tokens and signs it with the local keys, refreshing if required
//
// i for one would love not to need to do any of this, but here we are
//
// this is not, and will never be internet facing.
//

public class AtProtoProxy
{
    public static void Map(WebApplication app)
    {
        app.Map("/xrpc/{**catchall}", ProxyHandler);
    }

    private static async Task ProxyHandler(HttpContext context,
                                           LiveDbContext dbContext,
                                           IJWKProvider jwkProvider,
                                           IServiceProvider services,
                                           UserManager<LiveUser> userManager,
                                           IConnectedServicesContainer connectedServices,
                                           IHttpMessageHandlerFactory factory,
                                           string catchall)
    {
        try
        {
            using var scope = services.CreateScope();

            var connectionId = context.Request.Headers["X-Connection-ID"].ToString();

            var user = await userManager.GetUserAsync(context.User)
                ?? throw new InvalidOperationException("Invalid user was specified.");

            var guid = Guid.Parse(connectionId);
            var service = await dbContext.ConnectedServices.FirstOrDefaultAsync(s => s.Id == guid);

            // the service can't be used at this time, reject this request
            if (service == null ||
                service.Service != AtProto.SERVICE_NAME ||
                service.UserId != user.Id ||
                (service.Flags & LiveConnectedServiceFlags.Busted) == LiveConnectedServiceFlags.Busted)
                throw new InvalidOperationException("This ticket has expired.");

            if (!connectedServices.TryGetValue(service.Service, out var serviceDescription))
                throw new InvalidOperationException("This service is unsupported at this time.");

            if (service.ExpiresAt <= DateTime.UtcNow)
            {
                var handler = await serviceDescription.OAuthHandler(scope.ServiceProvider);
                if (!await handler.RefreshTokensAsync(service))
                    throw new InvalidOperationException();

                dbContext.ConnectedServices.Update(service);
                await dbContext.SaveChangesAsync();
            }

            var key = await jwkProvider.GetJWK(service.DPoPKeyId!);
            using var innerHandler = factory.CreateHandler();
            using var tokenHandler = new ProofTokenMessageHandler(key, innerHandler);

            using var client = new HttpClient(tokenHandler);
            var targetUrl = new Uri(new Uri(service.ServiceUrl!), "/xrpc/" + catchall + context.Request.QueryString);
            var targetRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

            // Copy headers except Host/Auth/DPoP
            foreach (var header in context.Request.Headers)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("DPoP", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("X-Connection-ID", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                    continue;

                targetRequest.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
            }

            targetRequest.Headers.Authorization = new AuthenticationHeaderValue("DPoP", service.AccessToken);

            if (context.Request.ContentLength > 0)
                targetRequest.Content = new StreamContent(context.Request.Body);

            using var resp = await client.SendAsync(targetRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

            context.Response.StatusCode = (int)resp.StatusCode;
            foreach (var header in resp.Headers)
            {
                if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                    continue;
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in resp.Content.Headers)
            {
                if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                    continue;
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            await resp.Content.CopyToAsync(context.Response.Body);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }
    }
}
