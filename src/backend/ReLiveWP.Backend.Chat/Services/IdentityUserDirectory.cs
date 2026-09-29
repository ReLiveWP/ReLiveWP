using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Chat.Services;

public class IdentityUserDirectory(User.UserClient users) : IUserDirectory
{
    public async Task<string?> FindUserIdByAddressAsync(string address, CancellationToken ct)
    {
        var request = new LookupUsersByEmailRequest { IncludePrivate = true };
        request.Emails.Add(address);

        var response = await users.LookupUsersByEmailAsync(request, cancellationToken: ct);
        return response.Users.FirstOrDefault()?.UserId;
    }
}
