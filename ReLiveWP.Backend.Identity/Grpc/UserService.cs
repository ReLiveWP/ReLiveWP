using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Identity.Data;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Identity.Grpc;

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

    public override async Task ListUsers(ListUsersRequest request, IServerStreamWriter<UserSummary> stream, ServerCallContext context)
    {
        var query = userManager.Users.Where(u => u.Type == LiveUserType.User);
        await foreach (var user in query.AsAsyncEnumerable().WithCancellation(context.CancellationToken))
        {
            await stream.WriteAsync(new UserSummary
            {
                Id = user.Id.ToString(),
                Username = user.UserName ?? "",
                EmailAddress = user.Email ?? "",
            });
        }
    }
}
