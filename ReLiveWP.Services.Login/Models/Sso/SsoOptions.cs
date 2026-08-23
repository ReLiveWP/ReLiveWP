namespace ReLiveWP.Services.Login.Models.Sso;

public class SsoOptions
{
    public const string SectionName = "Sso";

    public SsoCookieOptions Cookie { get; set; } = new();
    public Dictionary<string, SsoClient> Clients { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string PortalUrl { get; set; } = "https://relivewp.net/";
    public string SupportUrl { get; set; } = "https://support.relivewp.net/";

    public SsoClient? FindClient(string? clientId)
        => clientId is { Length: > 0 } && Clients.TryGetValue(clientId, out var client) ? client : null;
}

public class SsoCookieOptions
{
    public string Name { get; set; } = "__Host-RPSSecAuth";
}

public class SsoClient
{
    public string Name { get; set; } = "";
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> ServiceTargets { get; set; } = [];
    public bool RequirePkce { get; set; } = true;

    public bool IsRedirectUriAllowed(string? redirectUri)
        => redirectUri is { Length: > 0 } && RedirectUris.Contains(redirectUri, StringComparer.Ordinal);

    public bool IsPostLogoutRedirectUriAllowed(string? redirectUri)
        => redirectUri is { Length: > 0 } && PostLogoutRedirectUris.Contains(redirectUri, StringComparer.Ordinal);

    /// <summary>
    /// Client scoping only. This stops the marketplace client asking for an EAS token; it is not a
    /// per-user entitlement check, which the token backend does not currently have.
    /// </summary>
    public bool AreTargetsAllowed(IEnumerable<string> targets)
        => targets.All(t => ServiceTargets.Contains(t, StringComparer.Ordinal));
}
