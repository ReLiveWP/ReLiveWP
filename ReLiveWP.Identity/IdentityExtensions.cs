using System.Security.Claims;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Authorization;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Identity.LiveID;
using Microsoft.Extensions.DependencyInjection;
using ReLiveWP.Identity.Grpc;

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

    public class AddGrpcAuthenticationOptions
    {
        public AddGrpcAuthenticationOptions() { }
        public Action<GrpcAuthOptions>? GrpcConfiguration { internal get; set; }
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

    public static void AddGrpcAuthentication(this IServiceCollection collection, Action<AddGrpcAuthenticationOptions>? options = null)
    {
        var opts = new AddGrpcAuthenticationOptions();
        options?.Invoke(opts);

        collection.AddAuthentication(GrpcAuthHandler.SchemeName)
                  .AddScheme<GrpcAuthOptions, GrpcAuthHandler>(GrpcAuthHandler.SchemeName, opts.GrpcConfiguration);

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
}
