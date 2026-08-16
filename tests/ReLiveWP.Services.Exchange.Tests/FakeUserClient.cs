using Grpc.Core;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Exchange.Tests;

public class FakeUserClient : User.UserClient
{
    public List<UpdateUserProfileRequest> ProfileUpdates { get; } = [];
    public List<SetUserPictureRequest> PictureSets { get; } = [];
    public List<ClearUserPictureRequest> PictureClears { get; } = [];

    public override AsyncUnaryCall<UserProfile> UpdateUserProfileAsync(UpdateUserProfileRequest request, CallOptions options)
    {
        ProfileUpdates.Add(request);
        return Unary(() => new UserProfile { UserId = request.UserId });
    }

    public override AsyncUnaryCall<UserProfile> SetUserPictureAsync(SetUserPictureRequest request, CallOptions options)
    {
        PictureSets.Add(request);
        return Unary(() => new UserProfile { UserId = request.UserId });
    }

    public override AsyncUnaryCall<UserProfile> ClearUserPictureAsync(ClearUserPictureRequest request, CallOptions options)
    {
        PictureClears.Add(request);
        return Unary(() => new UserProfile { UserId = request.UserId });
    }

    private static AsyncUnaryCall<T> Unary<T>(Func<T> body) =>
        new(Task.FromResult(body()),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
}
