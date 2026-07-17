using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[Route("/Microsoft-Server-ActiveSync")]
public class ActiveSyncController : ControllerBase
{
    [HttpOptions]
    public IActionResult Options()
    {
        // we support AS 14.1 and this set of commands
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
