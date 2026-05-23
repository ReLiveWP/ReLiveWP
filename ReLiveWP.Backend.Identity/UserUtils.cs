namespace ReLiveWP.Backend.Identity;

public record struct UserIdentifiers(Guid UserId, string Cid, long Puid);

public class UserUtils
{
    public static UserIdentifiers GenerateUserId()
    {
        var userId = Guid.NewGuid();
        var chars = userId.ToString();
        var bytes = userId.ToByteArray();
        var time_low = BitConverter.ToUInt32(bytes, 0);
        var node = BitConverter.ToUInt32(bytes, 12);

        var cid = chars[19..23] + chars[24..36];
        var puid = (long)((ulong)time_low << 32 | node);

        return new UserIdentifiers(userId, cid, puid);
    }
}
