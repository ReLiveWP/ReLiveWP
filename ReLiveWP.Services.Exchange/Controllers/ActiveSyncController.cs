using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Controllers;

[ApiController]
[Route("/Microsoft-Server-ActiveSync")]
[Consumes("application/vnd.ms-sync", "application/vnd.ms-sync.wbxml")]
public class ActiveSyncController : ControllerBase
{
    private static HttpClient client
        = new HttpClient();

    private readonly ILogger<ActiveSyncController> _logger;

    public ActiveSyncController(ILogger<ActiveSyncController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task Post(
        [FromQuery] string User, 
        [FromQuery] string DeviceId,
        [FromQuery] string DeviceType,
        [FromQuery] string Cmd)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);

            var data = memoryStream.ToArray();
            _logger.LogInformation("Got bytes {bytes}", string.Join(" ", data.Select(c => c.ToString("X2"))));


            var decoder = new ASWBXML();
            decoder.LoadBytes(data);

            var xml = decoder.GetXmlDocument();
            _logger.LogInformation("Got XML {xml}", xml.OuterXml);


            Response.Headers.ContentType = "application/vnd.ms-sync.wbxml";

            switch (Cmd.ToLowerInvariant())
            {
                case "foldersync":
                    {
                        var serialiser = new XmlSerializer(typeof(FolderSync));
                        var result = (FolderSync)serialiser.Deserialize(new XmlNodeReader(xml))!;
                        var sync = new FolderSync()
                        {
                            SyncKey = "1",
                            Changes = new()
                            {
                                Count = 9,
                                Add =
                                {
                                    new()
                                    {
                                        Type = FolderType.InboxDefault,
                                        DisplayName = "Inbox",
                                        ServerId = "1",
                                        ParentId = "0"
                                    },
                                    new()
                                    {
                                        Type = FolderType.DraftsDefault,
                                        DisplayName = "Drafts",
                                        ServerId = "2",
                                        ParentId = "0"
                                    },
                                    new()
                                    {
                                        Type = FolderType.DeletedItemsDefault,
                                        DisplayName = "Deleted Items",
                                        ServerId = "3",
                                        ParentId = "0"
                                    },
                                    new()
                                    {
                                        Type = FolderType.SentItemsDefault,
                                        DisplayName = "Sent Items",
                                        ServerId = "4",
                                        ParentId = "0"
                                    },
                                    new()
                                    {
                                        Type = FolderType.TasksDefault,
                                        DisplayName = "Tasks",
                                        ServerId = "5",
                                        ParentId = "0"
                                    },
                                    new()
                                    {
                                        Type = FolderType.CalendarDefault,
                                        DisplayName = "Calendar",
                                        ServerId = "6",
                                        ParentId = "0"
                                    },
                                    new()
                                    {
                                        Type = FolderType.ContactsDefault,
                                        DisplayName = "Contacts",
                                        ServerId = "7",
                                        ParentId = "0"
                                    },
                                    new()
                                    {
                                        Type = FolderType.NotesDefault,
                                        DisplayName = "Notes",
                                        ServerId = "8",
                                        ParentId = "0"
                                    },
                                    new()
                                    {
                                        Type = FolderType.JournalDefault,
                                        DisplayName = "Journal",
                                        ServerId = "9",
                                        ParentId = "0"
                                    },
                                    new()
                                    {
                                        Type = FolderType.OutboxDefault,
                                        DisplayName = "Outbox",
                                        ServerId = "10",
                                        ParentId = "0"
                                    },
                                }
                            }
                        };

                        using var writer = new StringWriter();
                        serialiser.Serialize(writer, sync);

                        decoder = new ASWBXML();
                        decoder.LoadXml(writer.ToString());
                        var bytes = decoder.GetBytes();
                        decoder.LoadBytes(bytes);

                        _logger.LogInformation("Sent XML {xml}", decoder.GetXmlDocument().OuterXml);

                        await Response.Body.WriteAsync(bytes);
                        break;
                    }

                default:
                    {
                        _logger.LogError("Unsupported Command {command}", Cmd);
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read WB-XML");
        }
    }

    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers["MS-ASProtocolVersions"] = "14.1";
        Response.Headers["MS-ASProtocolCommands"] = "Sync,SendMail,SmartForward,SmartReply,GetAttachment,GetHierarchy,CreateCollection,DeleteCollection,MoveCollection,FolderSync,FolderCreate,FolderDelete,FolderUpdate,MoveItems,GetItemEstimate,MeetingResponse,Search,Settings,Ping,ItemOperations,Provision,ResolveRecipients,ValidateCert";

        return Ok();
    }
}
