using Microsoft.AspNetCore.Authentication;
using ReLiveWP.Identity;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;

namespace ReLiveWP.Services.Exchange.Middleware;

/// <summary>
/// Parses the EAS request URI (text or binary base64 format), decodes the WBXML
/// body (when present), and stores an <see cref="ActiveSyncContext"/> in
/// <see cref="HttpContext.Items"/> under <see cref="ContextKey"/>.
/// Must run before MVC so that <c>EasCommandAttribute</c> constraints can read it.
/// </summary>
public class ActiveSyncMiddleware(RequestDelegate next, ILogger<ActiveSyncMiddleware> logger, EasRequestLog requestLog)
{
    /// <summary>Key used to store <see cref="ActiveSyncContext"/> in <see cref="HttpContext.Items"/>.</summary>
    public const string ContextKey = "EasContext";

    private const string EasPath = "/Microsoft-Server-ActiveSync";

    public async Task InvokeAsync(HttpContext context)
    {
        ActiveSyncContext easContext = null;
        try
        {
            if (context.Request.Path.StartsWithSegments(EasPath, StringComparison.OrdinalIgnoreCase)
                && context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                // Buffer the body so controllers can read it after we consume it here.
                context.Request.EnableBuffering();

                easContext = new ActiveSyncContext
                {
                    ProtocolVersion = context.Request.Headers["MS-ASProtocolVersion"].FirstOrDefault(),
                };

                try
                {
                    ParseQueryString(context.Request, easContext);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse EAS query string: {Query}",
                        context.Request.QueryString.Value);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                logger.LogDebug("EAS {Command} from {User}/{DeviceId} ({DeviceType}), key={PolicyKey}",
                    easContext.Command, easContext.User, easContext.DeviceId, easContext.DeviceType, easContext.PolicyKey);

                await DecodeBodyAsync(context.Request, easContext);

                context.Items[ContextKey] = easContext;
            }

            await next(context);
        }
        finally
        {
            if (easContext != null)
                await requestLog.RecordAsync(easContext);
        }
    }

    // ── Query string parsing ─────────────────────────────────────────────────────

    private static void ParseQueryString(HttpRequest request, ActiveSyncContext ctx)
    {
        // Detect binary vs text format.
        // Text format always has a "Cmd" key; binary format is a single base64 blob.
        if (request.Query.ContainsKey("Cmd"))
            ParseTextQueryString(request, ctx);
        else
            ParseBinaryQueryString(request, ctx);
    }

    /// <summary>
    /// Text format: ?Cmd=FolderSync&amp;User=alice&amp;DeviceId=…&amp;DeviceType=…
    /// </summary>
    private static void ParseTextQueryString(HttpRequest request, ActiveSyncContext ctx)
    {
        var q = request.Query;

        ctx.Command = ParseCommandName(q["Cmd"].FirstOrDefault() ?? string.Empty);
        ctx.User = q["User"].FirstOrDefault();
        ctx.DeviceId = q["DeviceId"].FirstOrDefault() ?? string.Empty;
        ctx.DeviceType = q["DeviceType"].FirstOrDefault() ?? string.Empty;

        // Command-specific text parameters
        ctx.AttachmentName = q["AttachmentName"].FirstOrDefault();
        ctx.CollectionId = q["CollectionId"].FirstOrDefault();
        ctx.ItemId = q["ItemId"].FirstOrDefault();
        ctx.LongId = q["LongId"].FirstOrDefault();
        ctx.Occurrence = q["Occurrence"].FirstOrDefault();
        ctx.SaveInSent = string.Equals(q["SaveInSent"].FirstOrDefault(), "T",
                                StringComparison.OrdinalIgnoreCase);

        // Policy key can also be sent as a request header in text-format requests
        if (request.Headers.TryGetValue("X-MS-PolicyKey", out var pk)
            && uint.TryParse(pk.FirstOrDefault(), out var policyKey))
        {
            ctx.PolicyKey = policyKey;
        }
    }

    /// <summary>
    /// Binary format (MS-ASHTTP §2.2.1.1.1.1):
    /// <code>
    /// [0]     Protocol version (1 byte, e.g. 141 = 14.1)
    /// [1]     Command code (1 byte)
    /// [2–3]   Locale (2 bytes, little-endian)
    /// [4]     Device ID length
    /// [5…]    Device ID bytes
    /// [n]     Policy key length (0 or 4)
    /// [n+1…]  Policy key (4 bytes, optional, little-endian uint)
    /// [m]     Device type length
    /// [m+1…]  Device type bytes
    /// [rest]  Command parameters: Tag (1), Length (1), Value (variable) …
    /// </code>
    /// </summary>
    private static void ParseBinaryQueryString(HttpRequest request, ActiveSyncContext ctx)
    {
        var raw = request.QueryString.Value?.TrimStart('?') ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
            return;

        // Base64url and standard base64 both accepted — normalise padding
        var padded = raw.PadRight((raw.Length + 3) & ~3, '=');
        var bytes = Convert.FromBase64String(padded);
        var span = bytes.AsSpan();
        int pos = 0;

        if (span.Length < 4) return;

        // Byte 0: protocol version (e.g., 141 → "14.1")
        byte protocolByte = span[pos++];
        ctx.ProtocolVersion ??= FormatProtocolVersion(protocolByte);

        // Byte 1: command code
        byte commandCode = span[pos++];
        ctx.Command = (EasCommand)commandCode;

        // Bytes 2–3: locale (little-endian ushort, informational — not stored)
        pos += 2; // locale

        if (pos >= span.Length) return;

        // Device ID
        int devIdLen = span[pos++];
        if (devIdLen > 0 && pos + devIdLen <= span.Length)
        {
            ctx.DeviceId = Convert.ToHexString(span.Slice(pos, devIdLen));
            pos += devIdLen;
        }

        if (pos >= span.Length) return;

        // Policy key (0 or 4 bytes)
        int pkLen = span[pos++];
        if (pkLen == 4 && pos + 4 <= span.Length)
        {
            ctx.PolicyKey = BitConverter.ToUInt32(span.Slice(pos, 4));
            pos += 4;
        }
        else
        {
            pos += pkLen; // skip unexpected length
        }

        if (pos >= span.Length) return;

        // Device type
        int devTypeLen = span[pos++];
        if (devTypeLen > 0 && pos + devTypeLen <= span.Length)
        {
            ctx.DeviceType = System.Text.Encoding.UTF8.GetString(span.Slice(pos, devTypeLen));
            pos += devTypeLen;
        }

        // Command parameters: TLV sequence (MS-ASHTTP §2.2.1.1.1.1.1)
        // Tag values (§2.2.1.1.1.4):
        //   0 = AttachmentName, 3 = ItemId, 4 = LongId, 6 = Occurrence,
        //   7 = Options (bitmask: 0x01=SaveInSent, 0x02=AcceptMultiPart), 8 = User
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
                    // Options is a single bitmask byte, not a string
                    if (length == 1)
                    {
                        byte opts = span[pos - 1]; // already advanced; re-read the byte
                        ctx.SaveInSent = (opts & 0x01) != 0;
                        ctx.AcceptMultiPart = (opts & 0x02) != 0;
                    }
                    break;
                case 8: ctx.User = value; break;
                    // Unknown tags: skip silently
            }
        }
    }

    // ── Body decoding ────────────────────────────────────────────────────────────

    private async Task DecodeBodyAsync(HttpRequest request, ActiveSyncContext ctx)
    {
        var contentType = request.ContentType ?? string.Empty;

        // Only decode WBXML bodies; leave MIME/XML bodies for the controller to handle.
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

            logger.LogDebug("Decoded WBXML body: {Xml}", ctx.XmlDocument.OuterXml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decode WBXML body for command {Command}", ctx.Command);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

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
