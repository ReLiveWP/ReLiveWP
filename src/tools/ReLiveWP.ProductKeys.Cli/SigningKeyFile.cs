using System.Text;
using System.Text.Json;
using ReLiveWP.ProductKeys;

namespace ReLiveWP.ProductKeys.Cli;

public static class SigningKeyFile
{
    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ReLiveWP", "product-keys", "private-key.json");

    public const string DefaultIniSectionName = "ProductKeys";

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private sealed record SigningKeyDocument(
        string P, string A, string B,
        string GeneratorX, string GeneratorY,
        string PublicKeyX, string PublicKeyY,
        string GeneratorOrder, string PrivateKey);

    public static void Save(string path, ProductKeySigningKey signingKey)
    {
        var curve = signingKey.Curve;
        var generator = curve.Generator.Normalize();
        var publicKey = curve.PublicKey.Normalize();

        var document = new SigningKeyDocument(
            ProductKeyCurve.FormatHex(curve.P),
            ProductKeyCurve.FormatHex(curve.A),
            ProductKeyCurve.FormatHex(curve.B),
            ProductKeyCurve.FormatHex(generator.AffineXCoord.ToBigInteger()),
            ProductKeyCurve.FormatHex(generator.AffineYCoord.ToBigInteger()),
            ProductKeyCurve.FormatHex(publicKey.AffineXCoord.ToBigInteger()),
            ProductKeyCurve.FormatHex(publicKey.AffineYCoord.ToBigInteger()),
            ProductKeyCurve.FormatHex(signingKey.GeneratorOrder),
            ProductKeyCurve.FormatHex(signingKey.PrivateKey));

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(document, SerializerOptions);
        File.WriteAllText(path, json);
    }

    public static ProductKeySigningKey Load(string path)
    {
        if (!File.Exists(path))
            throw new CommandLineException($"no signing key at {path}, run new-curve first or pass --key-file.");

        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<SigningKeyDocument>(json)
            ?? throw new CommandLineException($"{path} is empty.");

        var curve = ProductKeyCurve.FromHex(document.P, document.A, document.B,
                                            document.GeneratorX, document.GeneratorY,
                                            document.PublicKeyX, document.PublicKeyY);

        var signingKey = new ProductKeySigningKey(curve,
                                                  ProductKeyCurve.ParseHex(document.GeneratorOrder),
                                                  ProductKeyCurve.ParseHex(document.PrivateKey));
        signingKey.EnsureConsistent();
        return signingKey;
    }

    public static string FormatIniSection(ProductKeyCurve curve, string sectionName)
    {
        var generator = curve.Generator.Normalize();
        var publicKey = curve.PublicKey.Normalize();

        var builder = new StringBuilder();
        builder.AppendLine($"[{sectionName}]");
        builder.AppendLine("Required=true");
        builder.AppendLine($"P={ProductKeyCurve.FormatHex(curve.P)}");
        builder.AppendLine($"A={ProductKeyCurve.FormatHex(curve.A)}");
        builder.AppendLine($"B={ProductKeyCurve.FormatHex(curve.B)}");
        builder.AppendLine($"GeneratorX={ProductKeyCurve.FormatHex(generator.AffineXCoord.ToBigInteger())}");
        builder.AppendLine($"GeneratorY={ProductKeyCurve.FormatHex(generator.AffineYCoord.ToBigInteger())}");
        builder.AppendLine($"PublicKeyX={ProductKeyCurve.FormatHex(publicKey.AffineXCoord.ToBigInteger())}");
        builder.AppendLine($"PublicKeyY={ProductKeyCurve.FormatHex(publicKey.AffineYCoord.ToBigInteger())}");
        return builder.ToString();
    }
}
