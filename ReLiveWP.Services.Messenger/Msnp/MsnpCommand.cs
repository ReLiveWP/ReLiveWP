using System.Text;

namespace ReLiveWP.Services.Messenger.Msnp;

public class MsnpCommand(string verb, string trId, string[] arguments)
{
    public string Verb { get; } = verb;
    public string TrId { get; } = trId;
    public string[] Arguments { get; } = arguments;
    public string? Payload { get; private init; }

    public static MsnpCommand Create(string verb, string trId, params string[] arguments) =>
        new(verb, trId, arguments);

    public MsnpCommand WithPayload(string payload) => new(Verb, TrId, Arguments) { Payload = payload };

    public static bool TryParse(string line, out MsnpCommand command)
    {
        command = null!;

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 1)
            return false;

        command = new MsnpCommand(parts[0], parts.Length > 1 ? parts[1] : "", parts.Length > 2 ? parts[2..] : []);
        return true;
    }

    public string Serialize()
    {
        var head = Arguments.Length == 0 ? $"{Verb} {TrId}" : $"{Verb} {TrId} {string.Join(' ', Arguments)}";
        if (Payload is null)
            return head;

        var byteLength = Encoding.UTF8.GetByteCount(Payload);
        return $"{head} {byteLength}\r\n{Payload}";
    }
}
