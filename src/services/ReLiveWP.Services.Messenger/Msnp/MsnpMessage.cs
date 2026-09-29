using System.Globalization;
using System.Text;

namespace ReLiveWP.Services.Messenger.Msnp;

public class MsnpMessage
{
    private static readonly HashSet<string> PayloadVerbs =
        new(["ADL", "DEL", "PUT", "QRY", "RML", "SDG"], StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MsnpCommand> Commands { get; }

    public MsnpMessage(IReadOnlyList<MsnpCommand> commands)
    {
        Commands = commands;
    }

    public static MsnpMessage Of(params MsnpCommand[] commands) => new(commands);

    public static bool TryParse(ReadOnlySpan<byte> body, int maxCommands, out MsnpMessage message)
    {
        message = null!;
        var commands = new List<MsnpCommand>();

        var pos = 0;
        while (pos < body.Length)
        {
            var remaining = body[pos..];
            var lineLength = remaining.IndexOf("\r\n"u8);
            var line = lineLength < 0 ? remaining : remaining[..lineLength];
            pos += lineLength < 0 ? remaining.Length : lineLength + 2;

            if (line.IsEmpty)
                continue;

            if (commands.Count >= maxCommands)
                return false;

            if (!MsnpCommand.TryParse(Encoding.UTF8.GetString(line), out var command))
                return false;

            if (!PayloadVerbs.Contains(command.Verb))
            {
                commands.Add(command);
                continue;
            }

            if (command.Arguments is not [.., var lengthText]
                || !int.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out var payloadLength)
                || payloadLength > body.Length - pos)
                return false;

            var payload = body.Slice(pos, payloadLength).ToArray();
            pos += payloadLength;

            commands.Add(MsnpCommand.Create(command.Verb, command.TrId, command.Arguments[..^1]).WithPayload(payload));
        }

        message = new MsnpMessage(commands);
        return true;
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        foreach (var command in Commands)
            command.WriteTo(stream);

        return stream.ToArray();
    }
}
