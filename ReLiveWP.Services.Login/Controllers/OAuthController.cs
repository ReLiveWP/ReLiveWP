using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Login.Controllers
{
    [ApiController]
    [Route("oauth/[action]/{service?}")]
    public class OAuthController(ConnectedServices.ConnectedServicesClient oAuthClient) : Controller
    {
        public record BeginAcountLinkModel(string Service, string? Identifier = null);
        public record BeginAccountLinkResponse(string RedirectUri);

        [HttpPost]
        [Authorize]
        [ActionName("begin-account-link")]
        public async Task<ActionResult<BeginAccountLinkResponse>> BeginAccountLink([FromBody] BeginAcountLinkModel model)
        {
            var headers = new Metadata
            {
                { "Authorization", Request.Headers.Authorization.ToString() }
            };

            var response = await oAuthClient.BeginAccountLinkingForServiceAsync(
                new() { Service = model.Service, Identifer = model.Identifier },
                headers);

            return new BeginAccountLinkResponse(response.RedirectUri);
        }

        [AllowAnonymous]
        [ActionName("callback")]
        public async Task<ActionResult> OAuthCallback(string service, string state, string issuer = "", string code = null, string error = null, string error_description = null)
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
                Code = code
            };

            await oAuthClient.FinaliseAccountLinkingForServiceAsync(request);

            return Redirect("https://int.relivewp.net/login-complete");
        }

        [AllowAnonymous]
        [ActionName("jwks")]
        public async Task<ActionResult> GetPubKeys()
        {
            var response = await oAuthClient.GetJsonWebKeysAsync(new Empty());
            return Content(response.Keys, "application/json");
        }
    }
}
