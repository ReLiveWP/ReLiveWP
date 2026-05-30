using System.Globalization;
using System.Security.Cryptography;
using ReLiveWP.Backend.Identity.Data;

namespace ReLiveWP.Backend.Identity;

public record struct UserIdentifiers(Guid UserId, string Cid, long Puid);

public class UserUtils
{
    public static UserIdentifiers GenerateUserIds(LiveUserType userType)
    {
        var userId = Guid.NewGuid();
        var chars = userId.ToString();
        var cid = ((long)(ulong.Parse(chars[19..23] + chars[24..36], NumberStyles.HexNumber) & 0x7FFFFFFFFFFFFFFF))
            .ToString("x");
        var puid = GeneratePuid(userType);

        return new UserIdentifiers(userId, cid, puid);
    }

    // thank you billy i owe you a drink
    public static bool IsValidPuid(LiveUserType userType, ulong id)
    {
        return userType switch
        {
            LiveUserType.User => (id & 0xffff000000000000) == 0,
            LiveUserType.Device => (id & 0xFC00000000000000) != 0xFC00000000000000,
            _ => throw new InvalidOperationException("Invalid LiveUserType")
        };
    }

    private static long GeneratePuid(LiveUserType userType)
    {
        ulong returnId;
        do
        {
            // Generate a secure random ulong
            byte[] buffer = new byte[8];
            RandomNumberGenerator.Fill(buffer);
            returnId = BitConverter.ToUInt64(buffer, 0);
        } while (!IsValidPuid(userType, returnId));

        // Set the "User" flag in the ID

        if (userType == LiveUserType.User)
            returnId |= 0x0009000000000000;

        if (userType == LiveUserType.Device)
            returnId |= 0xFC00000000000000;

        return (long)returnId;
    }
}


