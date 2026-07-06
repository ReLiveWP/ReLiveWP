using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Backend.SkyDrive.Services;

public class GooglePhotosProxyClient(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IPhotoSyncProxyClient
{
    public const string ServiceName = "google";
    private const string Host = "photoslibrary.googleapis.com";

    public string ServiceId => ServiceName;

    public async Task<ProviderUploadResult> UploadAsync(string connectionId, string authorization, PhotoUpload photo, CancellationToken ct = default)
    {
        var proxyBase = configuration["Endpoints:ConnectedServices:Proxy"]!.TrimEnd('/');
        using var client = httpClientFactory.CreateClient();

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, $"{proxyBase}/proxy/{ServiceName}/{Host}/v1/uploads")
        {
            Content = new ByteArrayContent(photo.Data)
        };
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        uploadRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-Protocol", "raw");
        uploadRequest.Headers.TryAddWithoutValidation("X-Goog-Upload-File-Name", photo.FileName);
        uploadRequest.Headers.TryAddWithoutValidation("X-Connection-ID", connectionId);
        uploadRequest.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var uploadResponse = await client.SendAsync(uploadRequest, ct);
        var uploadToken = (await uploadResponse.Content.ReadAsStringAsync(ct)).Trim();
        if (!uploadResponse.IsSuccessStatusCode || string.IsNullOrEmpty(uploadToken))
            throw new InvalidOperationException($"Google Photos upload failed ({(int)uploadResponse.StatusCode}): {uploadToken}");

        var batchBody = new
        {
            newMediaItems = new[]
            {
                new
                {
                    description = photo.Description ?? "",
                    simpleMediaItem = new
                    {
                        fileName = photo.FileName,
                        uploadToken
                    }
                }
            }
        };

        using var batchRequest = new HttpRequestMessage(HttpMethod.Post, $"{proxyBase}/proxy/{ServiceName}/{Host}/v1/mediaItems:batchCreate")
        {
            Content = JsonContent.Create(batchBody)
        };
        batchRequest.Headers.TryAddWithoutValidation("X-Connection-ID", connectionId);
        batchRequest.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var batchResponse = await client.SendAsync(batchRequest, ct);
        var json = await batchResponse.Content.ReadAsStringAsync(ct);
        if (!batchResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Google Photos batchCreate failed ({(int)batchResponse.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("newMediaItemResults")[0];

        if (!result.TryGetProperty("mediaItem", out var mediaItem))
        {
            var status = result.TryGetProperty("status", out var s) ? s.ToString() : "unknown";
            throw new InvalidOperationException($"Google Photos batchCreate returned no media item: {status}");
        }

        var id = mediaItem.GetProperty("id").GetString()!;
        var url = mediaItem.TryGetProperty("productUrl", out var p) ? p.GetString() : null;

        return new ProviderUploadResult(id, url);
    }
}
