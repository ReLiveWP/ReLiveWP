using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync", "application/vnd.ms-sync.wbxml")]
public class ActiveSyncController : ControllerBase
{
    private readonly ILogger<ActiveSyncController> _logger;

    public ActiveSyncController(ILogger<ActiveSyncController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        try
        {
            MemoryStream memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);

            var data = memoryStream.ToArray();
            _logger.LogInformation("Got bytes {bytes}", string.Join(" ", data.Select(c => c.ToString("X2"))));


            var decoder = new ASWBXML();
            decoder.LoadBytes(data);
            var xml = decoder.GetXml();
            _logger.LogInformation("Got XML {xml}", xml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read WB-XML");
        }

        return NoContent();
    }

    [HttpOptions]
    public async Task<IActionResult> Options()
    {
        Response.Headers["MS-ASProtocolVersions"] = "14.1";
        Response.Headers["MS-ASProtocolCommands"] = "Sync,SendMail,SmartForward,SmartReply,GetAttachment,GetHierarchy,CreateCollection,DeleteCollection,MoveCollection,FolderSync,FolderCreate,FolderDelete,FolderUpdate,MoveItems,GetItemEstimate,MeetingResponse,Search,Settings,Ping,ItemOperations,Provision,ResolveRecipients,ValidateCert";

        return Ok();
    }
}
