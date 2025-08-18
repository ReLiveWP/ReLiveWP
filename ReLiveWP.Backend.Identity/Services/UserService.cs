using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Identity.Services;

public class UserService(UserManager<LiveUser> userManager) : User.UserBase
{
    public override async Task<GetUserInfoResponse> GetUserInfo(GetUserInfoRequest request, ServerCallContext context)
    {
        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "User does not exist."));

        var chars = user.Id.ToString();
        var bytes = user.Id.ToByteArray();
        var time_low = BitConverter.ToUInt32(bytes, 0);
        var node = BitConverter.ToUInt32(bytes, 12);

        var cid = chars[19..23] + chars[24..36];
        var puid = ((ulong)time_low << 32) | node;

        var response = new GetUserInfoResponse()
        {
            Cid = cid,
            Puid = puid,
            Username = user.UserName,
            EmailAddress = user.Email
        };

        return response;
    }
}
