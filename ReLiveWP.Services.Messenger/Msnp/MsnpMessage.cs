namespace ReLiveWP.Services.Messenger.Msnp;

public class MsnpMessage
{
    public IReadOnlyList<MsnpCommand> Commands { get; }

    public MsnpMessage(IReadOnlyList<MsnpCommand> commands)
    {
        Commands = commands;
    }

    public static MsnpMessage Of(params MsnpCommand[] commands) => new(commands);

    public static bool TryParse(string body, out MsnpMessage message)
    {
        message = null!;
        var commands = new List<MsnpCommand>();

        var pos = 0;
        while (pos < body.Length)
        {
            var newlineIndex = body.IndexOf("\r\n", pos, StringComparison.Ordinal);
            var lineEnd = newlineIndex < 0 ? body.Length : newlineIndex;
            var line = body[pos..lineEnd];
            pos = newlineIndex < 0 ? body.Length : newlineIndex + 2;

            if (line.Length == 0)
                continue;

            if (!MsnpCommand.TryParse(line, out var command))
                return false;

            // PUT gets special cased through its included byte length
            if (command.Verb.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                && command.Arguments is [var lengthText]
                && int.TryParse(lengthText, out var length) && length > 0)
            {
                if (pos + length > body.Length)
                    return false;

                command = command.WithPayload(body.Substring(pos, length));
                pos += length;

                if (pos + 2 <= body.Length && body[pos] == '\r' && body[pos + 1] == '\n')
                    pos += 2;
            }

            commands.Add(command);
        }

        message = new MsnpMessage(commands);
        return true;
    }

    public string Serialize()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var command in Commands)
            sb.Append(command.Serialize()).Append("\r\n");

        return sb.ToString();
    }
}
