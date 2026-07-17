using Microsoft.AspNetCore.Authentication;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Middleware;

// must run before MVC so EasCommandAttribute constraints can read the parsed context
public class ActiveSyncMiddleware(RequestDelegate next, ILogger<ActiveSyncMiddleware> logger, IConfiguration configuration)
{
    public const string ContextKey = "EasContext";
    private const string EasPath = "/Microsoft-Server-ActiveSync";
    private const int MaxLoggedXmlLength = 4000;

    // opt-in only: request/response XML carries message bodies, contacts, and other PII, so it
    // must never be logged by default on a public deployment. Enable via Eas:LogMessageBodies.
    private readonly bool logMessageBodies = configuration.GetValue<bool>("Eas:LogMessageBodies");

    public async Task InvokeAsync(HttpContext context)
    {
        ActiveSyncContext? ctx = null;
        try
        {
            if (!context.Request.Path.StartsWithSegments(EasPath, StringComparison.OrdinalIgnoreCase)
                || !context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            // Buffer the body so controllers can read it after we consume it here.
            context.Request.EnableBuffering();

            ctx = new ActiveSyncContext
            {
                ProtocolVersion = context.Request.Headers["MS-ASProtocolVersion"].FirstOrDefault(),
                AcceptMultiPart = string.Equals(
                    context.Request.Headers["MS-ASAcceptMultiPart"].FirstOrDefault(), "T",
                    StringComparison.OrdinalIgnoreCase),
            };

            try
            {
                ParseQueryString(context.Request, ctx);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse EAS query string: {Query}",
                    context.Request.QueryString.Value);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            logger.LogDebug("EAS {Command} from {User}/{DeviceId} ({DeviceType}), key={PolicyKey}",
                ctx.Command, ctx.User, ctx.DeviceId, ctx.DeviceType, ctx.PolicyKey);

            await DecodeBodyAsync(context.Request, ctx);

            context.Items[ContextKey] = ctx;

            await next(context);
        }
        finally
        {
            if (ctx != null)
            {
                if (logMessageBodies)
                    logger.LogDebug("{Command}, {DeviceId}, {InputXml}, {OutputXml}", ctx.Command.ToString(), ctx.DeviceId,
                        Truncate(ctx.XmlDocument?.OuterXml) ?? "No xml", Truncate(ctx.OutputXml) ?? "No xml");
                else
                    logger.LogDebug("{Command} from {DeviceId} completed", ctx.Command.ToString(), ctx.DeviceId);
            }
        }
    }

    private static string? Truncate(string? xml) => xml switch
    {
        null => null,
        { Length: <= MaxLoggedXmlLength } => xml,
        _ => $"{xml[..MaxLoggedXmlLength]}... [truncated, {xml.Length} chars total]",
    };

    private static void ParseQueryString(HttpRequest request, ActiveSyncContext ctx)
    {
        // text format always has a "Cmd" key; binary format is a single base64 blob
        if (request.Query.ContainsKey("Cmd"))
            ParseTextQueryString(request, ctx);
        else
            ParseBinaryQueryString(request, ctx);
    }

    private static void ParseTextQueryString(HttpRequest request, ActiveSyncContext ctx)
    {
        var q = request.Query;

        ctx.Command = ParseCommandName(q["Cmd"].FirstOrDefault() ?? string.Empty);
        ctx.User = q["User"].FirstOrDefault();
        ctx.DeviceId = q["DeviceId"].FirstOrDefault() ?? string.Empty;
        ctx.DeviceType = q["DeviceType"].FirstOrDefault() ?? string.Empty;

        ctx.AttachmentName = q["AttachmentName"].FirstOrDefault();
        ctx.CollectionId = q["CollectionId"].FirstOrDefault();
        ctx.ItemId = q["ItemId"].FirstOrDefault();
        ctx.LongId = q["LongId"].FirstOrDefault();
        ctx.Occurrence = q["Occurrence"].FirstOrDefault();
        ctx.SaveInSent = string.Equals(q["SaveInSent"].FirstOrDefault(), "T",
                                StringComparison.OrdinalIgnoreCase);

        // policy key can also arrive as a request header in text-format requests
        if (request.Headers.TryGetValue("X-MS-PolicyKey", out var pk)
            && uint.TryParse(pk.FirstOrDefault(), out var policyKey))
        {
            ctx.PolicyKey = policyKey;
        }
    }

    private static void ParseBinaryQueryString(HttpRequest request, ActiveSyncContext ctx)
    {
        var raw = request.QueryString.Value?.TrimStart('?') ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
            return;

        // base64url and standard base64 both accepted, normalize padding
        var padded = raw.PadRight((raw.Length + 3) & ~3, '=');
        var bytes = Convert.FromBase64String(padded);
        var span = bytes.AsSpan();
        int pos = 0;

        if (span.Length < 4) return;

        byte protocolByte = span[pos++];
        ctx.ProtocolVersion ??= FormatProtocolVersion(protocolByte);

        byte commandCode = span[pos++];
        ctx.Command = (EasCommand)commandCode;

        pos += 2;

        if (pos >= span.Length) return;

        int devIdLen = span[pos++];
        if (devIdLen > 0 && pos + devIdLen <= span.Length)
        {
            ctx.DeviceId = Convert.ToHexString(span.Slice(pos, devIdLen));
            pos += devIdLen;
        }

        if (pos >= span.Length) return;

        int pkLen = span[pos++];
        if (pkLen == 4 && pos + 4 <= span.Length)
        {
            ctx.PolicyKey = BitConverter.ToUInt32(span.Slice(pos, 4));
            pos += 4;
        }
        else
        {
            pos += pkLen;
        }

        if (pos >= span.Length) return;

        int devTypeLen = span[pos++];
        if (devTypeLen > 0 && pos + devTypeLen <= span.Length)
        {
            ctx.DeviceType = System.Text.Encoding.UTF8.GetString(span.Slice(pos, devTypeLen));
            pos += devTypeLen;
        }

        while (pos + 2 <= span.Length)
        {
            byte tag = span[pos++];
            byte length = span[pos++];

            if (pos + length > span.Length) break;

            var value = System.Text.Encoding.UTF8.GetString(span.Slice(pos, length));
            pos += length;

            switch (tag)
            {
                case 0: ctx.AttachmentName = value; break;
                case 3: ctx.ItemId = value; break;
                case 4: ctx.LongId = value; break;
                case 6: ctx.Occurrence = value; break;
                case 7:
                    // options is a single bitmask byte, not a string
                    if (length == 1)
                    {
                        byte opts = span[pos - 1]; // already advanced past it
                        ctx.SaveInSent = (opts & 0x01) != 0;
                        ctx.AcceptMultiPart |= (opts & 0x02) != 0;
                    }
                    break;
                case 8: ctx.User = value; break;
            }
        }
    }

    private async Task DecodeBodyAsync(HttpRequest request, ActiveSyncContext ctx)
    {
        var contentType = request.ContentType ?? string.Empty;

        // only decode WBXML bodies, leave MIME/XML bodies for the controller to handle
        if (!contentType.StartsWith("application/vnd.ms-sync", StringComparison.OrdinalIgnoreCase))
            return;

        if (request.ContentLength == 0 || !request.Body.CanRead)
            return;

        try
        {
            using var ms = new MemoryStream();
            await request.Body.CopyToAsync(ms);
            request.Body.Position = 0; // rewind for controllers that want raw bytes

            var bytes = ms.ToArray();
            if (bytes.Length == 0) return;

            var decoder = new ASWBXML();
            decoder.LoadBytes(bytes);
            ctx.XmlDocument = decoder.GetXmlDocument();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decode WBXML body for command {Command}", ctx.Command);
        }
    }

    private static EasCommand ParseCommandName(string name) => name.ToLowerInvariant() switch
    {
        "sync" => EasCommand.Sync,
        "sendmail" => EasCommand.SendMail,
        "smartforward" => EasCommand.SmartForward,
        "smartreply" => EasCommand.SmartReply,
        "getattachment" => EasCommand.GetAttachment,
        "foldersync" => EasCommand.FolderSync,
        "foldercreate" => EasCommand.FolderCreate,
        "folderdelete" => EasCommand.FolderDelete,
        "folderupdate" => EasCommand.FolderUpdate,
        "moveitems" => EasCommand.MoveItems,
        "getitemestimate" => EasCommand.GetItemEstimate,
        "meetingresponse" => EasCommand.MeetingResponse,
        "search" => EasCommand.Search,
        "settings" => EasCommand.Settings,
        "ping" => EasCommand.Ping,
        "itemoperations" => EasCommand.ItemOperations,
        "provision" => EasCommand.Provision,
        "resolverecipients" => EasCommand.ResolveRecipients,
        "validatecert" => EasCommand.ValidateCert,
        _ => throw new ArgumentException($"Unknown EAS command: '{name}'"),
    };

    private static string FormatProtocolVersion(byte b) => b switch
    {
        141 => "14.1",
        140 => "14.0",
        121 => "12.1",
        _ => b.ToString(),
    };
}
