namespace ReLiveWP.Services.Messenger.Msnp;

public static class MsnpLayeredBody
{
    public readonly record struct ParsedBody(IReadOnlyDictionary<string, string> Headers, string Content);

    public static ParsedBody Parse(string payload)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var pos = 0;
        while (pos < payload.Length)
        {
            bool sawHeader;
            (pos, sawHeader) = ParseBlock(payload, pos, headers);

            if (!sawHeader || headers.ContainsKey("Content-Length"))
                break;
        }

        return new ParsedBody(headers, payload[Math.Min(pos, payload.Length)..]);
    }

    private static (int Pos, bool SawHeader) ParseBlock(string payload, int pos, Dictionary<string, string> headers)
    {
        var sawHeader = false;

        while (pos < payload.Length)
        {
            var newlineIndex = payload.IndexOf("\r\n", pos, StringComparison.Ordinal);
            var lineEnd = newlineIndex < 0 ? payload.Length : newlineIndex;
            var line = payload[pos..lineEnd];
            pos = newlineIndex < 0 ? payload.Length : newlineIndex + 2;

            if (line.Length == 0)
                break;

            var colon = line.IndexOf(':');
            if (colon < 0)
                continue;

            headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            sawHeader = true;
        }

        return (pos, sawHeader);
    }
}
