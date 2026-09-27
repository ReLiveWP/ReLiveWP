namespace ReLiveWP.Services.Login.Models;

// everything the client hands us on the way in, off the query string and, on Windows 8.1,
// the form body too
public record InlineLoginContext
{
    public string Id { get; init; } = "";
    public string Platform { get; init; } = "";
    public string Mkt { get; init; } = "EN-US";
    public string Lc { get; init; } = "";
    public string Ctc { get; init; } = "";
    public string Cmn { get; init; } = "";
    public string Opid { get; init; } = "";
    public string Uaid { get; init; } = "";
    public string Win8Colors { get; init; } = "";

    public bool IsPhone => Platform.Contains("Phone", StringComparison.OrdinalIgnoreCase);
}

public record InlineLoginModel(InlineLoginContext Context, IReadOnlyList<string> Palette)
{
    public string Username { get; init; } = "";
    public uint? ErrorCode { get; init; }
    public string? Error { get; init; }
    public string? HelpUrl { get; init; }
}

// property names are the host's
public record InlineLoginSuccessModel(IReadOnlyDictionary<string, string> Properties);
