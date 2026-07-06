using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models;

namespace ReLiveWP.Services.Login.Controllers;

[ApiController]
[Route("oauth/[action]/{service?}")]
public class OAuthController(ConnectedServices.ConnectedServicesClient connectedServicesClient, IConfiguration configuration) : Controller
{
    [HttpGet]
    [Authorize]
    [ActionName("available-links")]
    public async IAsyncEnumerable<AvailableConnectedService> GetAvailableLinksAsync()
    {
        var available = await connectedServicesClient.GetSupportedConnectionsAsync(new Empty());
        foreach (var service in available.AvailableConnections)
        {
            yield return new AvailableConnectedService(service.Service, service.DisplayName, (uint)service.Capabilities);
        }
    }

    [HttpPost]
    [Authorize]
    [ActionName("begin-account-link")]
    public async Task<ActionResult<BeginAccountLinkResponse>> BeginAccountLink([FromBody] BeginAcountLinkModel model)
    {
        var response = await connectedServicesClient.BeginAccountLinkingForServiceAsync(
            new() { Service = model.Service, Identifer = model.Identifier });

        return new BeginAccountLinkResponse(response.RedirectUri);
    }

    [HttpPost]
    [Authorize]
    [ActionName("begin-relink")]
    public async Task<ActionResult<BeginAccountLinkResponse>> BeginRelink([FromBody] BeginRelinkModel model)
    {
        var response = await connectedServicesClient.BeginRelinkForConnectionAsync(
            new() { ConnectionId = model.ConnectionId });

        return new BeginAccountLinkResponse(response.RedirectUri);
    }

    [HttpPatch]
    [Authorize]
    [ActionName("link")]
    public async Task<IActionResult> UpdateLink(string connectionId, [FromBody] UpdateConnectedServiceModel model)
    {
        await connectedServicesClient.UpdateCapabilitiesAsync(new UpdateCapabilitiesRequest()
        {
            ConnectionId = connectionId,
            Capabilities = model.EnabledCapabilities
        });

        return Accepted();
    }

    [HttpDelete]
    [Authorize]
    [ActionName("link")]
    public async Task<ActionResult> DeleteLink(string connectionId)
    {
        await connectedServicesClient.DeleteConnectionAsync(
            new() { ConnectionId = connectionId });

        return NoContent();
    }

    [AllowAnonymous]
    [ActionName("callback")]
    public async Task<ActionResult> OAuthCallback(
        string service,
        string state,
        string issuer = "",
        string? code = null,
        string? error = null,
        string? error_description = null,
        string? scope = null)
    {
        if (code == null)
        {
            // error case
            return Unauthorized();
        }

        var request = new FinaliseAccountLinkingRequest()
        {
            Service = service,
            State = state,
            Issuer = issuer,
            Code = code,
        };

        if (!string.IsNullOrWhiteSpace(scope))
            request.Scopes.AddRange(scope.Split(' '));

        var result = await connectedServicesClient.FinaliseAccountLinkingForServiceAsync(request);

        return Redirect($"{configuration["OAuth:LoginCompleteUrl"]!}?connectionId={Uri.EscapeDataString(result.ConnectionId)}");
    }

    [AllowAnonymous]
    [ActionName("jwks")]
    public async Task<ActionResult> GetPubKeys()
    {
        var response = await connectedServicesClient.GetJsonWebKeysAsync(new Empty());
        return Content(response.Keys, "application/json");
    }
}
