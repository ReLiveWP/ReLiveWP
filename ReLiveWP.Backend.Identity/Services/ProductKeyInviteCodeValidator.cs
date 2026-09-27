using ReLiveWP.ProductKeys;

namespace ReLiveWP.Backend.Identity.Services;

public class ProductKeyInviteCodeValidator(IConfiguration configuration) : IInviteCodeValidator
{
    private readonly ProductKeyVerifier _verifier = new(LoadCurve(configuration.GetRequiredSection("InviteKeys")));

    public InviteCodeCheck CheckInviteCode(string inviteCode)
    {
        if (!_verifier.TryVerifyProductKey(inviteCode, out var fields))
            return new InviteCodeCheck(false, null);

        return new InviteCodeCheck(true, fields.Serial);
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
