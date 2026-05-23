using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Razor.Templating.Core;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models;

namespace ReLiveWP.Services.Login.Controllers;

[ApiController]
[Route("/RST2.srf")]
public class RST2Controller(
    ILogger<RST2Controller> logger,
    Authentication.AuthenticationClient authenticationClient) : ControllerBase
{
    private const string WSSE_NS = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

    [HttpPost]
    [Consumes("application/soap+xml")]
    public async Task<IActionResult> PostAsync([FromBody] XmlDocument document)
    {
        var nsManager = new XmlNamespaceManager(document.NameTable);
        nsManager.AddNamespace("wsse", WSSE_NS);
        nsManager.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");

        //LiveUser user = null;
        //if (User.Identity.IsAuthenticated)
        //{
        //    user = await _userManager.GetUserAsync(User);
        //}

        var username = document.SelectSingleNode($"//wsse:Username", nsManager)?.InnerText;
        var password = document.SelectSingleNode($"//wsse:Password", nsManager)?.InnerText;

        SecurityTokensRequest grpcRequest;
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            grpcRequest = new SecurityTokensRequest()
            {
                Username = username,
                Password = password,
            };
        }
        else if (HttpContext.Request.Headers.TryGetValue("Authorization", out var values) && values.FirstOrDefault() != null)
        {
            var value = values.FirstOrDefault()!;
            if (value.StartsWith("Bearer "))
                value = value[7..];

            grpcRequest = new SecurityTokensRequest()
            {
                Username = username,
                AuthToken = value
            };
        }
        else
        {
            return Unauthorized();
        }

        var expires = DateTimeOffset.UtcNow.AddDays(30);
        var domains = document.SelectNodes("//wsa:Address", nsManager);
        var tokens = new List<RST2Token>();
        foreach (XmlNode domain in domains)
        {
            var grpcTokenRequest = new SecurityTokenRequest()
            {
                ServicePolicy = "",
                ServiceTarget = domain.InnerText
            };

            grpcRequest.Requests.Add(grpcTokenRequest);
        }

        if (!grpcRequest.Requests.Any(t => t.ServiceTarget == "http://Passport.NET/tb"))
            grpcRequest.Requests.Add(new SecurityTokenRequest() { ServicePolicy = "LEGACY", ServiceTarget = "http://Passport.NET/tb" });

        var grpcResponse = await authenticationClient.GetSecurityTokensAsync(grpcRequest);
        Marshal.ThrowExceptionForHR((int)grpcResponse.Code); // TODO: fix all of this please god

        var securityTokens = new List<RST2Token>();
        foreach (var resp in grpcResponse.Tokens)
        {
            if (resp.ServiceTarget == "http://Passport.NET/tb")
                continue;

            securityTokens.Add(new RST2Token() { Domain = resp.ServiceTarget, Token = resp.Token, BinarySecret = resp.Token });
        }

        var legacyToken = grpcResponse.Tokens.FirstOrDefault(t => t.ServiceTarget == "http://Passport.NET/tb");
        if (legacyToken == null)
            throw new Exception();

        var jwtHandler = new JwtSecurityTokenHandler();
        var token = jwtHandler.ReadJwtToken(legacyToken.Token);
        var cid = token.Claims.First(c => c.Type == "cid").Value;
        var puid = token.Claims.First(c => c.Type == "puid").Value;
        var email = token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value;
        var preferredUsername = token.Claims.First(c => c.Type == JwtRegisteredClaimNames.PreferredUsername).Value;


        var model = new RST2Model()
        {
            CID = cid,
            PUIDHex = puid,

            TimeZ = XmlConvert.ToString(expires.AddDays(-1)),
            TomorrowZ = XmlConvert.ToString(expires),
            Time5MZ = XmlConvert.ToString(expires.AddDays(-1).AddMinutes(5)),

            Token = legacyToken.Token,
            Tokens = tokens.ToArray(),

            Username = preferredUsername,
            Email = email,
            FirstName = "Thomas",
            LastName = "May",
            IP = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
        };

        var template = await RazorTemplateEngine.RenderAsync("~/Views/RST2.cshtml", model);

        return Content(template);
    }
}
