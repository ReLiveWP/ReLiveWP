using System.Security.Cryptography;

namespace ReLiveWP.Services.Login.Services;

public record SignupKey(string Ski, RSA Rsa);

public class SignupKeyRing
{
    private readonly List<SignupKey> _keys;

    public SignupKeyRing(IConfiguration configuration, ILogger<SignupKeyRing> logger)
    {
        _keys = LoadKeys(configuration);

        if (_keys.Count == 0)
            logger.LogWarning("no Signup:Keys configured, on-device signup is disabled");
    }

    public SignupKey? CurrentKey => _keys.FirstOrDefault();

    public SignupKey? FindKeyBySki(string ski)
        => _keys.FirstOrDefault(key => string.Equals(key.Ski, ski, StringComparison.OrdinalIgnoreCase));

    public static string ComputeSki(RSA rsa)
    {
        var subjectPublicKeyInfo = rsa.ExportSubjectPublicKeyInfo();
        return Convert.ToHexString(SHA1.HashData(subjectPublicKeyInfo));
    }

    private static List<SignupKey> LoadKeys(IConfiguration configuration)
    {
        var sections = configuration.GetSection("Signup:Keys")
            .GetChildren()
            .OrderBy(section => int.Parse(section.Key));

        var keys = new List<SignupKey>();
        foreach (var section in sections)
        {
            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(section["PrivateKey"]!), out _);
            keys.Add(new SignupKey(ComputeSki(rsa), rsa));
        }

        return keys;
    }
}
