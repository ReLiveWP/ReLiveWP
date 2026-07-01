namespace ReLiveWP.Services.Devices.Services;

public static class DeviceImageResolver
{
    public const string DefaultImage = "RM-801";

    private record DeviceImageRule(string[] ModelMatches, string ImageName);

    // first matching rule wins, falls back to DefaultImage
    private static readonly DeviceImageRule[] Rules =
    [
        new(["HD2", "PD29100"], "HD2"),
        new(["PI39110"], "PI39110"),
        new(["PI06100"], "PI06100"),
        new(["CETUS"], "CETUS"),
        new(["RM-803", "RM-809"], "RM-803"),
        new(["RM-808", "RM-823"], "RM-808"),
        new(["RM-835", "RM-836", "RM-849"], "RM-835"),
    ];

    public static string Resolve(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return DefaultImage;

        var normalised = model.ToUpperInvariant();
        foreach (var rule in Rules)
        {
            foreach (var match in rule.ModelMatches)
            {
                if (normalised.Contains(match))
                    return rule.ImageName;
            }
        }

        return DefaultImage;
    }
}
