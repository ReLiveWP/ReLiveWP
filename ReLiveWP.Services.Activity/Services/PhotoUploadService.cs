using Grpc.Core;
using ReLiveWP.Services.Grpc;
using ContentRangeHeaderValue = System.Net.Http.Headers.ContentRangeHeaderValue;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace ReLiveWP.Services.Activity.Services;

public record UploadedPhoto(string ResourceRef, string FileName, string MediaType);

public class PhotoUploadService(SkyDrive.SkyDriveClient skyDrive,
                                IHttpClientFactory httpClientFactory,
                                ILogger<PhotoUploadService> logger)
{
    // null when SkyDrive won't take the photo, either up front or once it has seen the outcomes
    public async Task<UploadedPhoto?> UploadAsync(Stream spool, PhotoUploadMetadata metadata, CancellationToken ct = default)
    {
        BeginPhotoUploadReply plan;
        try
        {
            plan = await skyDrive.BeginPhotoUploadAsync(new BeginPhotoUploadRequest
            {
                Metadata = metadata,
                ContentLength = spool.Length,
            }, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.FailedPrecondition or StatusCode.InvalidArgument)
        {
            return null;
        }

        var complete = new CompletePhotoUploadRequest { Metadata = metadata };
        foreach (var target in plan.Targets)
            complete.Outcomes.Add(await SendAsync(target, spool, ct));

        try
        {
            var created = await skyDrive.CompletePhotoUploadAsync(complete, cancellationToken: ct);
            return new UploadedPhoto(created.ResourceRef, created.FileName, created.MediaType);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            return null;
        }
    }

    // a failed target is recorded rather than thrown so one broken provider can't sink an upload the
    // others accepted; CompletePhotoUpload decides whether anything usable came back.
    private async Task<UploadOutcome> SendAsync(UploadTarget target, Stream spool, CancellationToken ct)
    {
        var outcome = new UploadOutcome { Service = target.Service, ConnectionId = target.ConnectionId };

        try
        {
            using var http = httpClientFactory.CreateClient();

            var total = spool.Length;
            var fragment = target.FragmentSize > 0 ? target.FragmentSize : total;

            for (var offset = 0L; offset < total; offset += fragment)
            {
                var length = Math.Min(fragment, total - offset);

                spool.Seek(offset, SeekOrigin.Begin);

                using var request = new HttpRequestMessage(new HttpMethod(target.Method), target.Url)
                {
                    Content = new StreamContent(new WindowStream(spool, length))
                };

                foreach (var (name, value) in target.Headers)
                {
                    if (!request.Headers.TryAddWithoutValidation(name, value))
                        request.Content.Headers.TryAddWithoutValidation(name, value);
                }

                request.Content.Headers.ContentLength = length;

                if (target.FragmentSize > 0)
                    request.Content.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + length - 1, total);

                using var response = await http.SendAsync(request, ct);

                outcome.StatusCode = (int)response.StatusCode;
                outcome.ResponseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upload to {Service} ({ConnectionId}) failed", target.Service, target.ConnectionId);
            outcome.StatusCode = 0;
            outcome.ResponseBody = "";
        }

        return outcome;
    }
}
