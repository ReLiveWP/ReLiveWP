namespace ReLiveWP.Services.Login.Models;

public record SkyDriveSettingsContext
{
    public string Mkt { get; init; } = "EN-US";
    public string Smkt { get; init; } = "";
    public string OSInfo { get; init; } = "";
    public string FormFactor { get; init; } = "";
    public string Timezone { get; init; } = "";
    public string Win8Colors { get; init; } = "";
}
