namespace ReLiveWP.Backend.SkyDrive.Services;

public record PhotoUpload(string FileName, string ContentType, string? Description, byte[] Data);
public record ProviderUploadResult(string ItemId, string? Url);

public interface IPhotoSyncProxyClient
{
    string ServiceId { get; }

    Task<ProviderUploadResult> UploadAsync(string connectionId, string authorization, PhotoUpload photo, CancellationToken ct = default);
}
