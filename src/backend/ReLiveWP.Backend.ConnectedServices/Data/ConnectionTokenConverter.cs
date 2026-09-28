using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReLiveWP.Backend.ConnectedServices.Services;

namespace ReLiveWP.Backend.ConnectedServices.Data;

public class ConnectionTokenConverter(ConnectionSecretProtector protector)
    : ValueConverter<string, string>(token => ProtectToken(protector, token),
                                     stored => UnprotectToken(protector, stored))
{
    public const string ProtectedPrefix = "enc1:";

    public static bool IsProtected(string stored)
        => stored.StartsWith(ProtectedPrefix, StringComparison.Ordinal);

    private static string ProtectToken(ConnectionSecretProtector protector, string token)
    {
        if (token.Length == 0)
            return token;

        var ciphertext = protector.Protect(token);
        return ProtectedPrefix + ciphertext;
    }

    private static string UnprotectToken(ConnectionSecretProtector protector, string stored)
    {
        if (!IsProtected(stored))
            return stored;

        return protector.Unprotect(stored[ProtectedPrefix.Length..]);
    }
}
