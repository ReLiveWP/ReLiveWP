using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Razor.Templating.Core;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models.RST2;

namespace ReLiveWP.Services.Login.Controllers;

[ApiController]
[Route("/RST2.srf")]
public class RST2Controller(
    ILogger<RST2Controller> logger,
    IRazorTemplateEngine razorTemplateEngine,
    Authentication.AuthenticationClient authenticationClient) : ControllerBase
{
    private const string WSSE_NS = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string WSU_NS = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string WSA_NS = "http://www.w3.org/2005/08/addressing";
    private const string WST_NS = "http://schemas.xmlsoap.org/ws/2005/02/trust";
    private const string WSP_NS = "http://schemas.xmlsoap.org/ws/2004/09/policy";
    private const string XENC_NS = "http://www.w3.org/2001/04/xmlenc#";

    private const string LegacyDaTokenTarget = "http://Passport.NET/tb";

    private const uint PPCRL_AUTHSTATE_E_UNAUTHENTICATED = 0x80048800;

    [HttpPost]
    public async Task<IActionResult> PostAsync()
    {
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            body = await reader.ReadToEndAsync();

        var document = new XmlDocument { PreserveWhitespace = true };
        try
        {
            document.LoadXml(body);
        }
        catch (XmlException ex)
        {
            logger.LogWarning(ex, "RST2 request was not well-formed XML");
            return BadRequest();
        }

        var ns = new XmlNamespaceManager(document.NameTable);
        ns.AddNamespace("wsse", WSSE_NS);
        ns.AddNamespace("wsa", WSA_NS);
        ns.AddNamespace("wsp", WSP_NS);
        ns.AddNamespace("xenc", XENC_NS);
        ns.AddNamespace("wst", WST_NS);

        var grpcRequest = BuildAuthenticatedRequest(document, ns, Request.Headers.Authorization);
        if (grpcRequest is null)
            return await ErrorRST2(0x8004882e); // PPCRL_REQUEST_E_MISSING_PRIMARY_CREDENTIAL

        foreach (XmlNode node in document.SelectNodes("//wst:RequestSecurityToken", ns) ?? EmptyList)
        {
            var supportingData = ByteString.Empty;
            var endpointReference = node.SelectSingleNode(".//wsa:EndpointReference", ns);
            if (endpointReference is null) continue;

            var policyRef = node.SelectSingleNode(".//wsp:PolicyReference", ns)?.Attributes?.GetNamedItem("URI")?.InnerText ?? "LEGACY";
            if (policyRef == "MBI_X509_DID")
            {
                // a certificate request should be in wst:Supporting
                var csr = node.SelectSingleNode(".//wsse:BinarySecurityToken", ns)?.InnerText;
                if (string.IsNullOrEmpty(csr))
                    throw new InvalidOperationException(); // TODO: return something nice

                supportingData = ByteString.FromBase64(csr);
            }

            var address = endpointReference.SelectSingleNode(".//wsa:Address", ns)?.InnerText;
            if (!string.IsNullOrEmpty(address))
            {
                grpcRequest.Requests.Add(new SecurityTokenRequest() { ServicePolicy = policyRef, ServiceTarget = address, SupportingData = supportingData });
            }
            else
            {
                var serviceName = endpointReference.SelectSingleNode(".//wsa:ServiceName", ns)?.InnerText;
                if (!string.IsNullOrEmpty(serviceName))
                {
                    grpcRequest.Requests.Add(new SecurityTokenRequest() { ServicePolicy = policyRef, ServiceTarget = serviceName, SupportingData = supportingData });
                }
            }
        }

        if (grpcRequest.Requests.Count == 0)
        {
            logger.LogWarning("RST2 request had no AppliesTo targets");
            return BadRequest();
        }

        var grpcResponse = await authenticationClient.GetSecurityTokensAsync(grpcRequest);
        if (grpcResponse.Code != 0)
        {
            // TODO: render a proper psf:pp SOAP fault (reqstatus/authstate) so the device shows the right error.
            logger.LogWarning("GetSecurityTokens failed for {User}: 0x{Code:X8}", grpcRequest.Username, grpcResponse.Code);
            return await ErrorRST2(grpcResponse.Code);
        }

        var legacyToken = grpcResponse.Tokens.FirstOrDefault(t => t.ServiceTarget == LegacyDaTokenTarget);
        if (legacyToken is not null)
            return Content(await RenderBootstrapAsync(grpcResponse, legacyToken), "application/soap+xml");

        if (!grpcResponse.HasSessionKey || grpcResponse.SessionKey.IsEmpty)
        {
            logger.LogWarning("Steady-state RST2 for {User} returned no session key from Identity", grpcRequest.Username);
            return await ErrorRST2(PPCRL_AUTHSTATE_E_UNAUTHENTICATED);
        }

        var daTokenId = DeviceDaTokenId(document, ns);
        var responseXml = await RenderServiceTokensAsync(grpcResponse, grpcResponse.SessionKey.ToByteArray(), daTokenId);
        return Content(responseXml, "application/soap+xml");
    }


    private static string DeviceDaTokenId(XmlDocument document, XmlNamespaceManager ns)
    {
        if (document.SelectSingleNode("//wsse:Security//xenc:EncryptedData", ns) is not XmlElement enc)
            return "DAToken";

        var id = enc.GetAttribute("Id");
        if (string.IsNullOrEmpty(id))
            id = enc.GetAttribute("Id", WSU_NS);
        return string.IsNullOrEmpty(id) ? "DAToken" : id;
    }

    private static SecurityTokensRequest? BuildAuthenticatedRequest(XmlDocument document, XmlNamespaceManager ns, string? authorization)
    {
        // if specified, extract the device auth token and decrypt it
        var daToken = document.SelectSingleNode("//wsse:Security//xenc:EncryptedData/xenc:CipherData/xenc:CipherValue", ns)?.InnerText;
        if (!string.IsNullOrWhiteSpace(daToken))
            return new SecurityTokensRequest() { DeviceAuthToken = daToken.Trim() };

        // otherwise, we're assuming username/password
        var username = document.SelectSingleNode("//wsse:Username", ns)?.InnerText;
        var password = document.SelectSingleNode("//wsse:Password", ns)?.InnerText;
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            return new SecurityTokensRequest() { Username = username, Password = password };

        // fallback, do we have an auth header?
        // TODO: do we need this?
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            var value = authorization.StartsWith("Bearer ") ? authorization[7..] : authorization;
            return new SecurityTokensRequest() { Username = username ?? "", AuthToken = value };
        }

        return null;
    }

    private async Task<string> RenderBootstrapAsync(SecurityTokensResponse response, SecurityTokenResponse legacyToken)
    {
        var created = legacyToken.Created.ToDateTimeOffset();
        var expires = legacyToken.Expires.ToDateTimeOffset();

        var model = new RST2Model()
        {
            CID = response.Cid,
            PUIDHex = response.Puid.ToString("X2").PadLeft(16, '0'),

            TimeZ = FormatZ(created),
            TomorrowZ = FormatZ(expires),
            Time5MZ = FormatZ(created.AddMinutes(5)),

            Token = legacyToken.Token,
            CipherValue = legacyToken.Token,
            ProofToken = legacyToken.ProofKey.ToBase64(),
            DaTokenReference = GenerateReferenceForBlob(legacyToken.Token),

            // any non-tb targets return compact service tokens; none are present in the tb bootstrap.
            Tokens = ServiceTokens(response),

            Username = response.Username,
            Email = response.EmailAddress,
            FirstName = FirstNameFrom(response),
            LastName = "",
            IP = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
        };

        return await razorTemplateEngine.RenderAsync("~/Views/RST2.cshtml", model);
    }

    private async Task<string> RenderServiceTokensAsync(SecurityTokensResponse response, byte[] sessionKey, string daTokenId)
    {
        var tokens = ServiceTokens(response);
        var created = tokens.Length > 0 ? tokens[0].Created : DateTimeOffset.UtcNow;
        var expires = tokens.Length > 0 ? tokens[0].Expires : created.AddDays(1);

        var model = new RST2Model()
        {
            CID = response.Cid,
            PUIDHex = response.Puid.ToString("X2").PadLeft(16, '0'),

            TimeZ = FormatZ(created),
            TomorrowZ = FormatZ(expires),
            Time5MZ = FormatZ(created.AddMinutes(5)),
            Tokens = tokens,

            Username = response.Username,
            Email = response.EmailAddress,
            FirstName = FirstNameFrom(response),
            LastName = "",
            IP = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
        };

        // RST2ServiceClear.cshtml pulls in the psf:pp header and the RSTR body as partials.
        return await razorTemplateEngine.RenderAsync("~/Views/RST2ServiceClear.cshtml", model);
    }

    private static RST2Token[] ServiceTokens(SecurityTokensResponse response) =>
    [
        .. response.Tokens
            .Where(t => t.ServiceTarget != LegacyDaTokenTarget)
            .Select((t, i) => new RST2Token()
            {
                Id = $"Compact{i}",
                Domain = t.ServiceTarget,
                Token = t.Token,
                Type = t.TokenType,
                Reference = t.TokenType != "MBI_X509_DID" ? GenerateReferenceForString($"t={t.Token}&p=") : GenerateReferenceForBlob(t.Token),
                Created = t.Created.ToDateTimeOffset(),
                Expires = t.Expires.ToDateTimeOffset(),
                ProofToken = t.HasProofKey ? t.ProofKey.ToBase64() : null
            })
    ];

    private static string FormatZ(DateTimeOffset value)
        => value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
    private static string GenerateReferenceForBlob(string base64Token)
        => Convert.ToBase64String(SHA1.HashData(Convert.FromBase64String(base64Token)));
    private static string GenerateReferenceForString(string token)
        => Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(token)));

    private static string FirstNameFrom(SecurityTokensResponse response)
    {
        // this decidedly does not work but we dont have first/last names right now
        var member = response.EmailAddress ?? response.Username ?? "";
        var at = member.IndexOf('@');
        return at > 0 ? member[..at] : member;
    }

    private static readonly XmlNodeList EmptyList = new XmlDocument().ChildNodes;

    private async Task<FileStreamResult> ErrorRST2(uint code)
    {
        var model = new RST2FailureModel(code);
        var template = await razorTemplateEngine.RenderAsync("~/Views/RST2.invalid.cshtml", model);

        return Xml(template);
    }

    private FileStreamResult Xml(string template)
    {
        var doc = new XmlDocument { PreserveWhitespace = false };
        doc.LoadXml(template);

        var transform = new XmlDsigExcC14NTransform(false);
        transform.LoadInput(doc);

        var ms = (MemoryStream)transform.GetOutput(typeof(Stream));
        return File(ms, "application/soap+xml");
    }

}
