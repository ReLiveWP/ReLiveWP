using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Backend.Mailbox.Tests;

public class FakeUserClient : User.UserClient
{
    // for tests that only care about the store, not about linking
    public static ContactLinkResolver NoLinks(MailboxDbContext db) =>
        new(db,
            new FakeUserClient { OnLookupUsersByEmail = _ => new LookupUsersByEmailResponse() },
            new ConfigurationBuilder().Build(),
            NullLogger<ContactLinkResolver>.Instance);

    public Func<GetUserPictureRequest, GetUserPictureResponse>? OnGetUserPicture { get; set; }
    public Func<GetUserProfileRequest, UserProfile>? OnGetUserProfile { get; set; }

    public int GetUserPictureCalls { get; private set; }

    public override AsyncUnaryCall<GetUserPictureResponse> GetUserPictureAsync(GetUserPictureRequest request, CallOptions options)
    {
        GetUserPictureCalls++;
        return Unary(() => OnGetUserPicture is null
            ? throw new RpcException(new Status(StatusCode.NotFound, "no picture"))
            : OnGetUserPicture(request));
    }

    public override AsyncUnaryCall<UserProfile> GetUserProfileAsync(GetUserProfileRequest request, CallOptions options)
    {
        if (OnGetUserProfile is null)
            throw new InvalidOperationException($"{nameof(OnGetUserProfile)} not configured");
        return Unary(() => OnGetUserProfile(request));
    }

    public Func<LookupUsersByEmailRequest, LookupUsersByEmailResponse>? OnLookupUsersByEmail { get; set; }

    public int LookupUsersByEmailCalls { get; private set; }

    public override AsyncUnaryCall<LookupUsersByEmailResponse> LookupUsersByEmailAsync(LookupUsersByEmailRequest request, CallOptions options)
    {
        if (OnLookupUsersByEmail is null)
            throw new InvalidOperationException($"{nameof(OnLookupUsersByEmail)} not configured");

        LookupUsersByEmailCalls++;
        return Unary(() => OnLookupUsersByEmail(request));
    }

    public static GetUserPictureResponse Picture(byte[] data, string etag) => new()
    {
        Data = ByteString.CopyFrom(data),
        ContentType = "image/jpeg",
        Etag = etag,
    };

    private static AsyncUnaryCall<T> Unary<T>(Func<T> body)
    {
        Task<T> task;
        try
        {
            task = Task.FromResult(body());
        }
        catch (Exception ex)
        {
            task = Task.FromException<T>(ex);
        }

        return new AsyncUnaryCall<T>(
            task,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}
