using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Authorization;
using ReLiveWP.Backend.Identity;
using ReLiveWP.Identity.LiveID;

namespace ReLiveWP.Identity;

public static class IdentityExtensions
{
    public class AddLiveIDAuthenticationOptions
    {
        public AddLiveIDAuthenticationOptions() { }

        public Action<GrpcClientFactoryOptions>? GrpcConfiguration { internal get; set; }
        public Action<LiveIDAuthOptions>? LiveIDConfiguration { internal get; set; }
        public Action<AuthorizationOptions>? AuthorizationConfiguration { internal get; set; }
    }

    public static void AddLiveIDAuthentication(this IServiceCollection collection, Action<AddLiveIDAuthenticationOptions> options)
    {
        var opts = new AddLiveIDAuthenticationOptions();
        options(opts);

        if (opts.GrpcConfiguration != null)
        {
            collection.AddGrpcClient<Authentication.AuthenticationClient>("Identity_GrpcClient", opts.GrpcConfiguration);
        }
        else
        {
            collection.AddGrpcClient<Authentication.AuthenticationClient>("Identity_GrpcClient");
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
}
