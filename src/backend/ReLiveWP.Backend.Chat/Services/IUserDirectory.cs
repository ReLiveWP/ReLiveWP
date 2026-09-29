namespace ReLiveWP.Backend.Chat.Services;

public interface IUserDirectory
{
    Task<string?> FindUserIdByAddressAsync(string address, CancellationToken ct);
}
