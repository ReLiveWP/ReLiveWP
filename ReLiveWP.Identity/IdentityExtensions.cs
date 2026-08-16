using System.Security.Claims;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using ReLiveWP.Identity.Exchange;
using ReLiveWP.Identity.Grpc;
using ReLiveWP.Identity.LiveID;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Identity;

public static class IdentityExtensions
{
    public class AddLiveIDAuthenticationOptions
    {
        public AddLiveIDAuthenticationOptions() { }

        public Action<GrpcClientFactoryOptions>? ConnectedServicesGrpcConfiguration { internal get; set; }
        public Action<LiveIDAuthOptions>? LiveIDConfiguration { internal get; set; }
        public Action<AuthorizationOptions>? AuthorizationConfiguration { internal get; set; }
    }

    public class AddExchangeAuthenticationOptions
    {
        public AddExchangeAuthenticationOptions() { }

        public Action<GrpcClientFactoryOptions>? IdentityGrpcConfiguration { internal get; set; }
        public Action<ExchangeAuthOptions>? ExchangeConfiguration { internal get; set; }
        public Action<AuthorizationOptions>? AuthorizationConfiguration { internal get; set; }
    }


    public class AddExchangeOrLiveIDAuthenticationOptions
    {
        public AddExchangeOrLiveIDAuthenticationOptions() { }

        public Action<GrpcClientFactoryOptions>? IdentityGrpcConfiguration { internal get; set; }
        public Action<ExchangeAuthOptions>? ExchangeConfiguration { internal get; set; }
        public Action<LiveIDAuthOptions>? LiveIDConfiguration { internal get; set; }
        public Action<AuthorizationOptions>? AuthorizationConfiguration { internal get; set; }
    }

    public class AddGrpcAuthenticationOptions
    {
        public AddGrpcAuthenticationOptions() { }
        public Action<GrpcAuthOptions>? GrpcConfiguration { internal get; set; }
        public Action<AuthorizationOptions>? AuthorizationConfiguration { internal get; set; }
    }

    public static void AddExchangeAuthentication(this IServiceCollection collection, Action<AddExchangeAuthenticationOptions> options)
    {
        var opts = new AddExchangeAuthenticationOptions();
        options(opts);

        if (opts.IdentityGrpcConfiguration != null)
        {
            collection.AddGrpcClient<Authentication.AuthenticationClient>("Identity_GrpcClient", opts.IdentityGrpcConfiguration);
        }
        else
        {
            collection.AddGrpcClient<Authentication.AuthenticationClient>("Identity_GrpcClient"); ;
        }


        collection.AddAuthentication(ExchangeAuthHandler.SchemeName)
                  .AddScheme<ExchangeAuthOptions, ExchangeAuthHandler>(ExchangeAuthHandler.SchemeName, opts.ExchangeConfiguration);

        if (opts.AuthorizationConfiguration != null)
        {
            collection.AddAuthorization(opts.AuthorizationConfiguration);
        }
        else
        {
            collection.AddAuthorization();
        }
    }

    public static void AddLiveIDAuthentication(this IServiceCollection collection, Action<AddLiveIDAuthenticationOptions> options)
    {
        var opts = new AddLiveIDAuthenticationOptions();
        options(opts);

        // LiveID tokens are verified in-process against the shared ES256 public key (JWT:PublicKey),
        // so no gRPC client to the Identity backend is needed here.
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

    public const string ExchangeOrLiveIDScheme = "ExchangeOrLiveID";

    public static void AddExchangeOrLiveIDAuthentication(this IServiceCollection collection, Action<AddExchangeOrLiveIDAuthenticationOptions> options)
    {
        var opts = new AddExchangeOrLiveIDAuthenticationOptions();
        options(opts);

        if (opts.IdentityGrpcConfiguration != null)
        {
            collection.AddGrpcClient<Authentication.AuthenticationClient>("Identity_GrpcClient", opts.IdentityGrpcConfiguration);
        }
        else
        {
            collection.AddGrpcClient<Authentication.AuthenticationClient>("Identity_GrpcClient");
        }

        collection.AddAuthentication(ExchangeOrLiveIDScheme)
                  .AddPolicyScheme(ExchangeOrLiveIDScheme, ExchangeOrLiveIDScheme, o =>
                  {
                      o.ForwardDefaultSelector = ctx =>
                      {
                          var auth = ctx.Request.Headers.Authorization.ToString();
                          if (auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                              return ExchangeAuthHandler.SchemeName;
                          if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                              return LiveIDAuthHandler.SchemeName;

                          // with no credentials a device must get WWW-Authenticate: Basic and a browser
                          // must not, or it raises a native auth modal. Origin is the only signal here.
                          return string.IsNullOrEmpty(ctx.Request.Headers.Origin)
                              ? ExchangeAuthHandler.SchemeName
                              : LiveIDAuthHandler.SchemeName;
                      };
                  })
                  .AddScheme<ExchangeAuthOptions, ExchangeAuthHandler>(ExchangeAuthHandler.SchemeName, opts.ExchangeConfiguration)
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
