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

        var response = new GetUserInfoResponse()
        {
            Cid = user.Cid,
            Puid = user.Puid,
            Username = user.UserName,
            EmailAddress = user.Email
        };

        return response;
    }
}
