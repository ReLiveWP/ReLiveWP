using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Services.Exchange.Controllers;

/// <summary>
/// Handles the HTTP OPTIONS request that clients use to discover which
/// protocol versions and commands the server supports (MS-ASHTTP §2.2.3).
/// All POST command requests are handled by per-command controllers that
/// derive from <see cref="ActiveSyncCommandController"/>.
/// </summary>
[ApiController]
[Route("/Microsoft-Server-ActiveSync")]
public class ActiveSyncController : ControllerBase
{
    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers["MS-ASProtocolVersions"] = "14.1,14.0,12.1";
        Response.Headers["MS-ASProtocolCommands"]  =
            "Sync,SendMail,SmartForward,SmartReply,GetAttachment," +
            "FolderSync,FolderCreate,FolderDelete,FolderUpdate," +
            "MoveItems,GetItemEstimate,MeetingResponse,Search," +
            "Settings,Ping,ItemOperations,Provision," +
            "ResolveRecipients,ValidateCert";

        return Ok();
    }
}
