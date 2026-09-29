using System.Globalization;
using System.Text;

namespace ReLiveWP.Services.Messenger.Msnp;

public class MsnpCommand(string verb, string trId, string[] arguments)
{
    private const string Redacted = "<redacted>";

    private static readonly HashSet<string> LayeredPayloadVerbs =
        new(["DEL", "NFY", "PUT", "SDG"], StringComparer.OrdinalIgnoreCase);

    private static readonly string[] LoggedPayloadHeaders =
        ["To", "From", "Service-Channel", "Uri", "Message-Type", "Content-Type"];

    public string Verb { get; } = verb;
    public string TrId { get; } = trId;
    public string[] Arguments { get; } = arguments;
    public byte[]? Payload { get; private init; }

    public string PayloadText => Payload is null ? "" : Encoding.UTF8.GetString(Payload);

    public static MsnpCommand Create(string verb, string trId, params string[] arguments) =>
        new(verb, trId, arguments);

    public static MsnpCommand Error(MsnpErrorCode code, string trId) =>
        new(((int)code).ToString(CultureInfo.InvariantCulture), trId, []);

    public MsnpCommand WithPayload(byte[] payload) => new(Verb, TrId, Arguments) { Payload = payload };

    public MsnpCommand WithPayload(string payload) => WithPayload(Encoding.UTF8.GetBytes(payload));

    public static bool TryParse(string line, out MsnpCommand command)
    {
        command = null!;

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1)
            return false;

        command = new MsnpCommand(parts[0], parts.Length > 1 ? parts[1] : "", parts.Length > 2 ? parts[2..] : []);
        return true;
    }

    public string ToLogString()
    {
        var line = new StringBuilder(Verb);
        if (TrId.Length > 0)
            line.Append(' ').Append(TrId);

        foreach (var argument in RedactCredentials(Arguments))
            line.Append(' ').Append(argument);

        if (Payload is null)
            return line.ToString();

        line.Append(" <").Append(Payload.Length.ToString(CultureInfo.InvariantCulture)).Append(" bytes>");
        if (!LayeredPayloadVerbs.Contains(Verb))
            return line.ToString();

        var headers = MsnpLayeredBody.Parse(PayloadText).Headers;
        foreach (var name in LoggedPayloadHeaders)
        {
            if (headers.TryGetValue(name, out var value))
                line.Append(' ').Append(name).Append('=').Append(value);
        }

        return line.ToString();
    }

    private IEnumerable<string> RedactCredentials(string[] arguments)
    {
        var isSsoResponse = Verb.Equals("USR", StringComparison.OrdinalIgnoreCase)
            && arguments is [var scheme, var stage, ..]
            && scheme.Equals("SSO", StringComparison.OrdinalIgnoreCase)
            && stage.Equals("S", StringComparison.OrdinalIgnoreCase);

        if (!isSsoResponse)
            return arguments;

        return arguments.Select((argument, index) => index < 2 || argument.StartsWith('{') ? argument : Redacted);
    }

    public void WriteTo(Stream stream)
    {
        var head = new StringBuilder(Verb);
        if (TrId.Length > 0)
            head.Append(' ').Append(TrId);

        foreach (var argument in Arguments)
            head.Append(' ').Append(argument);

        if (Payload is not null)
            head.Append(' ').Append(Payload.Length.ToString(CultureInfo.InvariantCulture));

        head.Append("\r\n");
        stream.Write(Encoding.UTF8.GetBytes(head.ToString()));

        if (Payload is not null)
            stream.Write(Payload);
    }
}
