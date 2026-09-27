using System.Globalization;

namespace ReLiveWP.ProductKeys.Cli;

public sealed class CommandLineException(string message) : Exception(message);

public sealed class CommandLineArguments
{
    private static readonly HashSet<string> FlagNames = ["--force", "--upgrade"];

    private readonly Dictionary<string, string> _options = [];
    private readonly HashSet<string> _flags = [];
    private readonly List<string> _positionals = [];

    public string? Command { get; }

    public CommandLineArguments(string[] args)
    {
        Command = args.Length > 0 ? args[0] : null;

        for (var i = 1; i < args.Length; i++)
        {
            var argument = args[i];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                _positionals.Add(argument);
                continue;
            }

            if (FlagNames.Contains(argument))
            {
                _flags.Add(argument);
                continue;
            }

            if (i + 1 >= args.Length)
                throw new CommandLineException($"{argument} needs a value.");

            _options[argument] = args[++i];
        }
    }

    public bool HasFlag(string name) => _flags.Contains(name);

    public string? GetOption(string name) => _options.GetValueOrDefault(name);

    public int? GetIntOption(string name)
    {
        var value = GetOption(name);
        if (value == null)
            return null;

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            throw new CommandLineException($"{name} wants a number, got '{value}'.");

        return number;
    }

    public int RequireIntOption(string name) =>
        GetIntOption(name) ?? throw new CommandLineException($"{name} is required.");

    public string RequirePositional(int index, string description) =>
        index < _positionals.Count ? _positionals[index] : throw new CommandLineException($"missing {description}.");
}
