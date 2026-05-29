using System.Security.Claims;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Authorization;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Identity.LiveID;
using Microsoft.Extensions.DependencyInjection;

namespace ReLiveWP.Identity;

public static class IdentityExtensions
{
    public class AddLiveIDAuthenticationOptions
    {
        public AddLiveIDAuthenticationOptions() { }

        public Action<GrpcClientFactoryOptions>? IdentityGrpcConfiguration { internal get; set; }
        public Action<GrpcClientFactoryOptions>? ConnectedServicesGrpcConfiguration { internal get; set; }
        public Action<LiveIDAuthOptions>? LiveIDConfiguration { internal get; set; }
        public Action<AuthorizationOptions>? AuthorizationConfiguration { internal get; set; }
    }

    public static void AddLiveIDAuthentication(this IServiceCollection collection, Action<AddLiveIDAuthenticationOptions> options)
    {
        var opts = new AddLiveIDAuthenticationOptions();
        options(opts);

        if (opts.IdentityGrpcConfiguration != null)
        {
            collection.AddGrpcClient<Authentication.AuthenticationClient>("Identity_GrpcClient", opts.IdentityGrpcConfiguration);
        }
        else
        {
            collection.AddGrpcClient<Authentication.AuthenticationClient>("Identity_GrpcClient");;
        }

        if (opts.ConnectedServicesGrpcConfiguration != null)
        {
            collection.AddGrpcClient<ConnectedServices.ConnectedServicesClient>("Identity_OAuthClient", opts.ConnectedServicesGrpcConfiguration);
        }
        else
        {
            collection.AddGrpcClient<ConnectedServices.ConnectedServicesClient>("Identity_OAuthClient");
        }

        collection.AddAuthentication(LiveIDAuthHandler.SchemeName)
                  .AddScheme<LiveIDAuthOptions, LiveIDAuthHandler>(LiveIDAuthHandler.SchemeName, opts.LiveIDConfiguration);


        if (opts.AuthorizationConfiguration != null)
        {
            collection.AddAuthorization(opts.AuthorizationConfiguration);
        }
        else
        {
            collection.AddAuthorization();
        }
    }

    public static string? Id(this ClaimsPrincipal? identity)
        => identity?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Returns the user's Windows Live PUID (Passport Unique ID), which is the
    /// 64-bit integer used as the CID (Contact ID) in Live People annotations.
    /// Sourced from the "puid" claim emitted by the Identity service.
    /// </summary>
    public static long? Puid(this ClaimsPrincipal? identity)
    {
        var value = identity?.Claims.FirstOrDefault(c => c.Type == "puid")?.Value;
        return value is not null && long.TryParse(value, out var puid) ? puid : null;
    }
}
