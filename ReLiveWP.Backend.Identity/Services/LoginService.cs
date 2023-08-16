using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ReLiveWP.Backend.Identity;
using ReLiveWP.Backend.Identity.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ReLiveWP.Backend.Identity.Services
{
    public class LoginService : Login.LoginBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginService> _logger;
        private readonly UserManager<LiveUser> _userManager;
        private readonly RoleManager<LiveRole> _roleManager;

        public LoginService(IConfiguration configuration,
                            ILogger<LoginService> logger,
                            UserManager<LiveUser> userManager,
                            RoleManager<LiveRole> roleManager)
        {
            _configuration = configuration;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private string NormaliseUsername(string username)
        {
            if (username.IndexOf('@') != -1)
                return username.Substring(0, username.IndexOf('@'));
            return username;
        }

        public override async Task<UserExistsResponse> UserExists(UserExistsRequest request, ServerCallContext context)
        {
            var user = await _userManager.FindByNameAsync(NormaliseUsername(request.Username));
            return new UserExistsResponse() { Exists = user != null };
        }

        public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
        {
            var user = await _userManager.FindByNameAsync(NormaliseUsername(request.Username));
            if (user == null)
                return new LoginResponse() { Succeeded = false };

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                return new LoginResponse() { Succeeded = false };

            var authClaims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in await _userManager.GetRolesAsync(user))
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));

            var expires = DateTimeOffset.UtcNow.AddDays(30);
            var token = GetToken(authClaims, expires);

            return new LoginResponse
            {
                Succeeded = true,
                Uuid = user.Id.ToString(),
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Exipres = Timestamp.FromDateTimeOffset(expires)
            };
        }

        public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            if (await _userManager.FindByEmailAsync(request.EmailAddress) != null)
                return new RegisterResponse() { Succeeded = false };

            var user = new LiveUser()
            {
                UserName = request.Username,
                Email = request.EmailAddress,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            return new RegisterResponse() { Succeeded = result.Succeeded };
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
    }
}