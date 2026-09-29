using System.Globalization;
using System.Text;

namespace ReLiveWP.Services.Messenger.Msnp;

public sealed record MsnpSdg(MsnpMuri To, MsnpMuri From, string ServiceChannel, string MessageType)
{
    public const string OfflineChannel = "IM/Offline";
    public const string OriginalArrivalTimeHeader = "Original-Arrival-Time";
    public const string OriginalArrivalTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    private static ReadOnlySpan<byte> OfflineChannelLine => "\r\nService-Channel: IM/Offline\r\n"u8;
    private static ReadOnlySpan<byte> OnlineChannelLine => "\r\nService-Channel: IM/Online\r\n"u8;
    private static ReadOnlySpan<byte> EndOfBlock => "\r\n\r\n"u8;
    private static ReadOnlySpan<byte> MessagingBlockStart => "\r\n\r\nMessaging:"u8;

    public bool IsOfflineChannel => ServiceChannel.Equals(OfflineChannel, StringComparison.OrdinalIgnoreCase);

    public static bool TryRead(MsnpCommand command, out MsnpSdg sdg)
    {
        sdg = null!;
        if (command.Payload is not { Length: > 0 })
            return false;

        var blocks = MsnpLayeredBody.ParseBlocks(command.PayloadText);
        if (blocks.Count == 0)
            return false;

        var routing = blocks[0];
        var messaging = blocks[^1];
        if (!MsnpMuri.TryParse(routing.GetValueOrDefault("To"), out var to)
            || !MsnpMuri.TryParse(routing.GetValueOrDefault("From"), out var from))
            return false;

        sdg = new MsnpSdg(
            to,
            from,
            routing.GetValueOrDefault("Service-Channel") ?? "",
            messaging.GetValueOrDefault("Message-Type") ?? "");
        return true;
    }

    public static byte[] WithOnlineChannel(byte[] payload)
    {
        var routingEnd = payload.AsSpan().IndexOf(EndOfBlock);
        var routing = routingEnd < 0 ? payload.AsSpan() : payload.AsSpan(0, routingEnd + EndOfBlock.Length);

        var at = routing.IndexOf(OfflineChannelLine);
        if (at < 0)
            return payload;

        var after = payload.AsSpan(at + OfflineChannelLine.Length);
        return [.. payload.AsSpan(0, at), .. OnlineChannelLine, .. after];
    }

    internal static bool TryFindMessagingHeaders(ReadOnlySpan<byte> payload, out int headersStart, out int headersLength)
    {
        headersStart = 0;
        headersLength = 0;

        var blockStart = payload.IndexOf(MessagingBlockStart);
        if (blockStart < 0)
            return false;

        headersStart = blockStart + EndOfBlock.Length;
        headersLength = payload[headersStart..].IndexOf(EndOfBlock);
        return headersLength >= 0;
    }

    internal static int MessagingBodyStart(int headersStart, int headersLength) => headersStart + headersLength + EndOfBlock.Length;

    public static byte[] WithOriginalArrivalTime(byte[] payload, DateTimeOffset arrival)
    {
        if (!TryFindMessagingHeaders(payload, out var headersStart, out var headersLength))
            return payload;

        var headers = Encoding.UTF8.GetString(payload, headersStart, headersLength)
            .Split("\r\n")
            .Where(line => !line.StartsWith(OriginalArrivalTimeHeader + ":", StringComparison.OrdinalIgnoreCase))
            .Append($"{OriginalArrivalTimeHeader}: {FormatArrivalTime(arrival)}");

        var rebuilt = Encoding.UTF8.GetBytes(string.Join("\r\n", headers));
        return [.. payload.AsSpan(0, headersStart), .. rebuilt, .. payload.AsSpan(headersStart + headersLength)];
    }

    public static string FormatArrivalTime(DateTimeOffset arrival) =>
        arrival.UtcDateTime.ToString(OriginalArrivalTimeFormat, CultureInfo.InvariantCulture);
}
