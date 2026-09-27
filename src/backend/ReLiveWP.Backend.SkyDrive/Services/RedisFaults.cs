using Grpc.Core;
using StackExchange.Redis;

namespace ReLiveWP.Backend.SkyDrive.Services;

internal static class RedisFaults
{
    public static RpcException Unavailable(RedisException ex) =>
        new(new Status(StatusCode.Unavailable, "Redis unavailable", ex));
}
