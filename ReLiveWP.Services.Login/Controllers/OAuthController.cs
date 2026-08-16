using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models;
using GrpcStatus = Grpc.Core.StatusCode;

namespace ReLiveWP.Services.Login.Controllers;

[ApiController]
[Route("oauth/[action]/{service?}")]
public class OAuthController(
    ConnectedServices.ConnectedServicesClient connectedServicesClient,
    IConfiguration configuration) : Controller
{
    [HttpGet]
    [Authorize]
    [ActionName("available-links")]
    public async IAsyncEnumerable<AvailableConnectedService> GetAvailableLinksAsync()
    {
        var available = await connectedServicesClient.GetSupportedConnectionsAsync(new Empty());
        foreach (var service in available.AvailableConnections)
        {
            yield return new AvailableConnectedService(
                service.Service, service.DisplayName, service.Capabilities, service.ShareableCapabilities);
        }
    }

    [HttpPost]
    [Authorize]
    [ActionName("begin-account-link")]
    public async Task<ActionResult<BeginAccountLinkResponse>> BeginAccountLink([FromBody] BeginAcountLinkModel model)
    {
        var response = await connectedServicesClient.BeginAccountLinkingForServiceAsync(
            new() { Service = model.Service, Identifer = model.Identifier, Transient = model.Transient });

        return new BeginAccountLinkResponse(response.RedirectUri);
    }

    [HttpPost]
    [Authorize]
    [ActionName("begin-relink")]
    public async Task<ActionResult<BeginAccountLinkResponse>> BeginRelink([FromBody] BeginRelinkModel model)
    {
        var request = new BeginRelinkRequest { ConnectionId = model.ConnectionId };

        if (model.RequestedCapabilities is { } requested)
            request.RequestedCapabilities = requested;

        var response = await connectedServicesClient.BeginRelinkForConnectionAsync(request);

        return new BeginAccountLinkResponse(response.RedirectUri);
    }

    [HttpPost]
    [Authorize]
    [ActionName("link-credentials")]
    public async Task<ActionResult<CredentialLinkResponse>> LinkCredentials([FromBody] CredentialLinkModel model)
    {
        var request = new CredentialLinkRequest()
        {
            Service = model.Service,
            ServiceUrl = model.ServiceUrl,
            Username = model.Username,
            Secret = model.Secret,
            Transient = model.Transient,
        };

        if (!string.IsNullOrEmpty(model.ConnectionId))
            request.ConnectionId = model.ConnectionId;

        if (!string.IsNullOrWhiteSpace(model.Label))
            request.Label = model.Label;

        try
        {
            var response = await connectedServicesClient.LinkServiceWithCredentialsAsync(request);
            return new CredentialLinkResponse(response.ConnectionId);
        }
        catch (RpcException ex) when (ex.StatusCode == GrpcStatus.InvalidArgument)
        {
            // the detail explains which part of the server or credentials was wrong
            return BadRequest(ex.Status.Detail);
        }
    }

    [HttpPatch]
    [Authorize]
    [ActionName("link")]
    public async Task<IActionResult> UpdateLink(string connectionId, [FromBody] UpdateConnectedServiceModel model)
    {
        var request = new UpdateCapabilitiesRequest()
        {
            ConnectionId = connectionId,
            Capabilities = model.EnabledCapabilities
        };

        if (model.SharedCapabilities is { } shared)
            request.SharedCapabilities = shared;

        await connectedServicesClient.UpdateCapabilitiesAsync(request);

        return Accepted();
    }

    [HttpDelete]
    [Authorize]
    [ActionName("link")]
    public async Task<ActionResult> DeleteLink(string connectionId, bool deleteData = false)
    {
        await connectedServicesClient.DeleteConnectionAsync(
            new() { ConnectionId = connectionId, DeleteData = deleteData });

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
