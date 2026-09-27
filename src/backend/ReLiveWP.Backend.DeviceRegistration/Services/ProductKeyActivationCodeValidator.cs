using ReLiveWP.ProductKeys;

namespace ReLiveWP.Backend.DeviceRegistration.Services;

public class ProductKeyActivationCodeValidator(IConfiguration configuration) : IActivationCodeValidator
{
    private readonly ProductKeyVerifier _verifier = new(LoadCurve(configuration.GetRequiredSection("ProductKeys")));

    public ActivationCodeCheck CheckActivationCode(string activationCode)
    {
        if (!_verifier.TryVerifyProductKey(activationCode, out var fields))
            return new ActivationCodeCheck(false, null);

        return new ActivationCodeCheck(true, fields.Serial);
    }

    private static ProductKeyCurve LoadCurve(IConfigurationSection section)
    {
        return ProductKeyCurve.FromHex(RequireValue(section, "P"),
                                       RequireValue(section, "A"),
                                       RequireValue(section, "B"),
                                       RequireValue(section, "GeneratorX"),
                                       RequireValue(section, "GeneratorY"),
                                       RequireValue(section, "PublicKeyX"),
                                       RequireValue(section, "PublicKeyY"));
    }

    private static string RequireValue(IConfigurationSection section, string key) =>
        section[key] ?? throw new InvalidOperationException($"{section.Path}:{key} is not configured.");
}
