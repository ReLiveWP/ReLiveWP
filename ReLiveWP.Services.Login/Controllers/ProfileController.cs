using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models;
using GrpcStatus = global::Grpc.Core.StatusCode;

namespace ReLiveWP.Services.Login.Controllers;

[ApiController]
public class ProfileController(
    User.UserClient userClient,
    IConfiguration configuration,
    ILogger<ProfileController> logger) : ControllerBase
{
    private const int MaxUploadBytes = 8 * 1024 * 1024;

    [Authorize]
    [HttpPatch("/auth/user/@me")]
    public async Task<ActionResult<UserModel>> UpdateProfile([FromBody] UpdateProfileModel request, CancellationToken cancellationToken)
    {
        var grpcRequest = new UpdateUserProfileRequest { UserId = User.Id() };
        if (request.FirstName is not null) grpcRequest.FirstName = request.FirstName;
        if (request.LastName is not null) grpcRequest.LastName = request.LastName;

        if (request.Visibility is not null)
        {
            if (VisibilityModel.FromModel(request.Visibility) is not { } visibility)
                return BadRequest(new ErrorModel(0, null));

            grpcRequest.Visibility = visibility;
        }

        var profile = await userClient.UpdateUserProfileAsync(grpcRequest, cancellationToken: cancellationToken);
        return ToModel(profile, configuration);
    }

    [Authorize]
    [HttpPut("/auth/user/@me/picture")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<ActionResult<UserModel>> SetPicture(
        IFormFile file,
        [FromForm(Name = "crop_x")] int? cropX,
        [FromForm(Name = "crop_y")] int? cropY,
        [FromForm(Name = "crop_size")] int? cropSize,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return BadRequest(new ErrorModel(0, null));

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ErrorModel(0, null));

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        var request = new SetUserPictureRequest
        {
            UserId = User.Id(),
            Data = ByteString.CopyFrom(buffer.ToArray()),
        };

        if (cropSize is { } size)
            request.Crop = new CropRect { X = cropX ?? 0, Y = cropY ?? 0, Size = size };

        try
        {
            var profile = await userClient.SetUserPictureAsync(request, cancellationToken: cancellationToken);
            return ToModel(profile, configuration);
        }
        catch (RpcException e) when (e.StatusCode == GrpcStatus.InvalidArgument)
        {
            logger.LogInformation("rejected an avatar upload: {Detail}", e.Status.Detail);
            return BadRequest(new ErrorModel(0, null));
        }
    }

    [Authorize]
    [HttpDelete("/auth/user/@me/picture")]
    public async Task<ActionResult<UserModel>> ClearPicture(CancellationToken cancellationToken)
    {
        var profile = await userClient.ClearUserPictureAsync(
            new ClearUserPictureRequest { UserId = User.Id() }, cancellationToken: cancellationToken);

        return ToModel(profile, configuration);
    }

    // anonymous: this is the url the WP7 tile fetch and profile.live.com's UserTileStaticUrl hit,
    // and neither carries a bearer token
    [AllowAnonymous]
    [HttpGet("/profile/{cid}/picture")]
    public async Task<IActionResult> GetPicture(string cid, [FromQuery] bool thumbnail, CancellationToken cancellationToken)
    {
        GetUserPictureResponse picture;
        try
        {
            picture = await userClient.GetUserPictureAsync(
                new GetUserPictureRequest { Cid = cid, Thumbnail = thumbnail }, cancellationToken: cancellationToken);
        }
        catch (RpcException e) when (e.StatusCode == GrpcStatus.NotFound)
        {
            return NotFound();
        }

        var etag = new EntityTagHeaderValue($"\"{picture.Etag}\"");
        Response.Headers.CacheControl = "public, max-age=300";

        return File(picture.Data.ToByteArray(), picture.ContentType, lastModified: null, entityTag: etag);
    }

    public static string? PictureUrl(IConfiguration configuration, string cid, string? etag)
    {
        if (etag is null)
            return null;

        var root = configuration["PublicUrls:Avatars"];
        return string.IsNullOrWhiteSpace(root) ? null : $"{root.TrimEnd('/')}/{cid}/picture";
    }

    private static UserModel ToModel(UserProfile profile, IConfiguration configuration)
    {
        var etag = profile.HasPictureEtag ? profile.PictureEtag : null;

        return new UserModel(
            profile.UserId,
            profile.Cid,
            profile.Puid,
            profile.Username,
            profile.EmailAddress,
            profile.HasFirstName ? profile.FirstName : null,
            profile.HasLastName ? profile.LastName : null,
            PictureUrl(configuration, profile.Cid, etag),
            etag,
            VisibilityModel.ToModel(profile.Visibility));
    }
}
