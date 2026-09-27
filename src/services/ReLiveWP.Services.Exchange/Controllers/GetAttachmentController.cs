using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Attributes;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

// legacy binary-query fallback for clients that predate ItemOperations FileReference fetch
[ApiController]
[EasCommand(EasCommand.GetAttachment)]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync.wbxml", "application/vnd.ms-sync")]
public class GetAttachmentController(
    ILogger<GetAttachmentController> logger,
    AttachmentService attachments) : ActiveSyncCommandController
{
    [HttpPost]
    public async Task Post(CancellationToken ct)
    {
        var userId = User.Id()!;
        var fileReference = EasContext.AttachmentName;

        var resolved = await attachments.ResolveAsync(userId, fileReference, null, ct);
        if (resolved.Data is not { } data)
        {
            logger.LogInformation("GetAttachment {FileReference} for {User}: {Status}",
                fileReference, userId, resolved.Status);

            HttpContext.Response.StatusCode = resolved.Status switch
            {
                AttachmentResolveStatus.InvalidRange => StatusCodes.Status400BadRequest,
                AttachmentResolveStatus.Empty => StatusCodes.Status204NoContent,
                AttachmentResolveStatus.IoFailure => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status404NotFound,
            };
            return;
        }

        HttpContext.Response.ContentType = data.ContentType;
        HttpContext.Response.ContentLength = data.Content.Length;
        await HttpContext.Response.Body.WriteAsync(data.Content, ct);
    }
}
