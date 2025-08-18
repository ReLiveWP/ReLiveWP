using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Identity;

public record ClientAssertionModel(string Type, string Value);

public static class DPoPHelpers
{
    public static DelegatingHandler GetDPoPMessageHandler(this IServiceProvider services, string connectionId, string token, HttpMessageHandler? innerHandler = null)
    {
        var grpcClientFactory = services.GetRequiredService<GrpcClientFactory>();
        var client = grpcClientFactory.CreateClient<ConnectedServices.ConnectedServicesClient>("Identity_OAuthClient");

        return new ProofTokenMessageHandler(client, connectionId, token, innerHandler ?? new HttpClientHandler());
    }

    public static Func<Task<ClientAssertionModel>> GetClientAssertionFunc(this IServiceProvider services, string connectionId, string token)
    {
        var grpcClientFactory = services.GetRequiredService<GrpcClientFactory>();
        var authClient = grpcClientFactory.CreateClient<ConnectedServices.ConnectedServicesClient>("Identity_OAuthClient");

        return async () =>
        {
            var clientAssertionRequest = new ClientAssertionRequest()
            {
                ConnectionId = connectionId
            };

            var metadata = new Metadata() { { "Authorization", token } };
            var proof = await authClient.GetClientAssertionAsync(clientAssertionRequest, metadata);

            return new ClientAssertionModel(proof.AssertionType, proof.AssertionValue);
        };
    } 
}
