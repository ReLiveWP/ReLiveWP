using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Controllers;

namespace ReLiveWP.Services.Login.Tests;

public class OAuthCallbackTests
{
    private const string LoginComplete = "https://relivewp.test/login-complete";

    private readonly FinalisingClient client = new();

    [Fact]
    public async Task A_finished_link_hands_the_connection_back()
    {
        client.Respond = _ => new FinaliseAccountLinkingResponse { ConnectionId = "conn-1" };

        Assert.Equal($"{LoginComplete}?connectionId=conn-1", await CallbackAsync(code: "abc"));
    }

    [Fact]
    public async Task Cancelling_on_the_provider_reads_as_denied()
    {
        Assert.Equal($"{LoginComplete}?error=denied", await CallbackAsync(code: null, error: "access_denied"));
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task Any_other_provider_error_reads_as_failed()
    {
        Assert.Equal($"{LoginComplete}?error=failed", await CallbackAsync(code: null, error: "server_error"));
    }

    [Fact]
    public async Task An_expired_ticket_reads_as_expired()
    {
        client.Respond = _ => throw new RpcException(new Status(StatusCode.Unauthenticated, "This ticket has expired."));

        Assert.Equal($"{LoginComplete}?error=expired", await CallbackAsync(code: "abc"));
    }

    [Theory]
    [InlineData(StatusCode.Internal)]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.FailedPrecondition)]
    public async Task Everything_else_reads_as_failed_and_keeps_the_detail_out_of_the_url(StatusCode status)
    {
        client.Respond = _ => throw new RpcException(new Status(status, "invalid_scope (secret detail)"));

        var url = await CallbackAsync(code: "abc");

        Assert.Equal($"{LoginComplete}?error=failed", url);
        Assert.DoesNotContain("secret", url);
    }

    private async Task<string> CallbackAsync(string? code, string? error = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OAuth:LoginCompleteUrl"] = LoginComplete })
            .Build();

        var controller = new OAuthController(client, configuration, NullLogger<OAuthController>.Instance);
        var result = await controller.OAuthCallback("mastodon", "state-1", code: code, error: error);

        return Assert.IsType<RedirectResult>(result).Url;
    }

    private sealed class FinalisingClient : ConnectedServices.ConnectedServicesClient
    {
        public Func<FinaliseAccountLinkingRequest, FinaliseAccountLinkingResponse> Respond { get; set; } =
            _ => throw new InvalidOperationException("not expected");

        public int Calls { get; private set; }

        public override AsyncUnaryCall<FinaliseAccountLinkingResponse> FinaliseAccountLinkingForServiceAsync(
            FinaliseAccountLinkingRequest request, CallOptions options)
        {
            Calls++;

            Task<FinaliseAccountLinkingResponse> response;
            try
            {
                response = Task.FromResult(Respond(request));
            }
            catch (Exception ex)
            {
                response = Task.FromException<FinaliseAccountLinkingResponse>(ex);
            }

            return new AsyncUnaryCall<FinaliseAccountLinkingResponse>(
                response, Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => [], () => { });
        }
    }
}
