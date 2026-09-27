using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace ReLiveWP.Services.Activity.Services;

// lets an <img> or a new tab fetch one photo at one size without being able to set a header
public class MediaTicketService
{
    public const string QueryKey = "t";
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    private readonly byte[] key;
    private readonly TimeProvider clock;

    public MediaTicketService(IConfiguration configuration, TimeProvider clock)
    {
        var secret = configuration["Media:TicketSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Media:TicketSecret is not configured.");

        key = Encoding.UTF8.GetBytes(secret);
        this.clock = clock;
    }

    public string SignUrl(string url, string userId, string resourceRef, int size)
        => $"{url}?{QueryKey}={MintTicket(userId, resourceRef, size)}";

    public string MintTicket(string userId, string resourceRef, int size)
    {
        var expiry = clock.GetUtcNow().Add(Lifetime).ToUnixTimeSeconds();
        var user = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(userId));
        var signature = WebEncoders.Base64UrlEncode(ComputeSignature(userId, resourceRef, size, expiry));

        return $"{user}.{expiry.ToString(CultureInfo.InvariantCulture)}.{signature}";
    }

    public bool TryVerifyTicket(string ticket, string resourceRef, int size, out string userId)
    {
        userId = "";

        var parts = ticket.Split('.');
        if (parts.Length != 3)
            return false;

        if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var expiry))
            return false;

        if (clock.GetUtcNow().ToUnixTimeSeconds() > expiry)
            return false;

        string presentedUser;
        byte[] presentedSignature;
        try
        {
            presentedUser = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(parts[0]));
            presentedSignature = WebEncoders.Base64UrlDecode(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = ComputeSignature(presentedUser, resourceRef, size, expiry);
        if (!CryptographicOperations.FixedTimeEquals(expected, presentedSignature))
            return false;

        userId = presentedUser;
        return true;
    }

    private byte[] ComputeSignature(string userId, string resourceRef, int size, long expiry)
    {
        var payload = FormattableString.Invariant($"{userId}\n{resourceRef}\n{size}\n{expiry}");
        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload));
    }
}
