using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Razor.Templating.Core;
using ReLiveWP.Backend.Identity;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Services.Authentication.Models;

namespace ReLiveWP.Services.Authentication.Controllers;

[ApiController]
[Route("/RST2.srf")]
public class RST2Controller : ControllerBase
{
    private const string WSSE_NS = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

    private readonly ILogger<RST2Controller> _logger;
    private readonly IConfiguration _configuration;
    private readonly UserManager<LiveUser> _userManager;
    private readonly RoleManager<LiveRole> _roleManager;
    private readonly SignInManager<LiveUser> _signInManager;

    public RST2Controller(ILogger<RST2Controller> logger,
                          IConfiguration configuration,
                          UserManager<LiveUser> userManager,
                          RoleManager<LiveRole> roleManager,
        SignInManager<LiveUser> signInManager)
    {
        _logger = logger;
        _configuration = configuration;
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
    }

    private string NormaliseUsername(string username)
    {
        if (username.IndexOf('@') != -1)
            return username.Substring(0, username.IndexOf('@'));
        return username;
    }

    private JwtSecurityToken GetToken(List<Claim> authClaims, DateTimeOffset expires)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

        var token = new JwtSecurityToken(
            expires: expires.UtcDateTime,
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return token;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        await _userManager.CreateAsync(new LiveUser() { UserName = "wamwoowam", Email = "wamwoowam@gmail.com" }, "Password1");

        return Ok();
    }

    [HttpPost]
    [Consumes("application/soap+xml")]
    public async Task<IActionResult> PostAsync([FromBody] XmlDocument document)
    {
        var nsManager = new XmlNamespaceManager(document.NameTable);
        nsManager.AddNamespace("wsse", WSSE_NS);
        nsManager.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");

        LiveUser user = null;
        if (User.Identity.IsAuthenticated)
        {
            user = await _userManager.GetUserAsync(User);
        }

        if (user == null)
        {
            var username = document.SelectSingleNode($"//wsse:Username", nsManager);
            var password = document.SelectSingleNode($"//wsse:Password", nsManager);

            if (username == null || password == null)
                return BadRequest();

            user = await _userManager.FindByNameAsync(NormaliseUsername(username.InnerText));
            if (user == null)
                return BadRequest();

            if (!await _userManager.CheckPasswordAsync(user, password.InnerText))
                return BadRequest();
        }

        var authClaims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var userRole in await _userManager.GetRolesAsync(user))
            authClaims.Add(new Claim(ClaimTypes.Role, userRole));

        var expires = DateTimeOffset.UtcNow.AddDays(30);
        var token = new JwtSecurityTokenHandler().WriteToken(GetToken(authClaims, expires));

        var chars = user.Id.ToString();
        var bytes = user.Id.ToByteArray();
        var time_low = BitConverter.ToUInt32(bytes, 0);
        var node = BitConverter.ToUInt32(bytes, 12);

        var cid = chars[19..23] + chars[24..36];
        var puid = ((ulong)time_low << 32) | node;

        var domains = document.SelectNodes("//wsa:Address", nsManager);
        var tokens = new List<RST2Token>();
        foreach (XmlNode domain in domains)
        {
            if (domain.InnerText == "http://Passport.NET/tb") continue;

            tokens.Add(new RST2Token()
            {
                Domain = domain.InnerText,
                Token = token,
                BinarySecret = token
            });
        }

        var model = new RST2Model()
        {
            CID = cid,
            PUIDHex = Convert.ToHexString(BitConverter.GetBytes(puid)),

            TimeZ = XmlConvert.ToString(expires.AddDays(-1)),
            TomorrowZ = XmlConvert.ToString(expires),
            Time5MZ = XmlConvert.ToString(expires.AddDays(-1).AddMinutes(5)),

            Token = token,
            Tokens = tokens.ToArray(),

            Username = user.UserName,
            Email = user.Email,
            FirstName = "Thomas",
            LastName = "May",
            IP = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
        };

        var template = await RazorTemplateEngine.RenderAsync("~/Views/RST2.cshtml", model);

        return Content(template);
    }
}
