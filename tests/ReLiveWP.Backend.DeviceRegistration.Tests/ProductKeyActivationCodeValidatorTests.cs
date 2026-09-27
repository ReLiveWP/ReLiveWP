using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Security;
using ReLiveWP.Backend.DeviceRegistration.Services;
using ReLiveWP.ProductKeys;

namespace ReLiveWP.Backend.DeviceRegistration.Tests;

public class ProductKeyActivationCodeValidatorTests
{
    private static readonly ProductKeySigningKey SigningKey =
        ProductKeyCurveGenerator.GenerateSigningKey(new SecureRandom());

    private static IConfiguration CreateConfiguration(ProductKeyCurve curve)
    {
        var generator = curve.Generator.Normalize();
        var publicKey = curve.PublicKey.Normalize();

        var values = new Dictionary<string, string?>
        {
            ["ProductKeys:Required"] = "true",
            ["ProductKeys:P"] = ProductKeyCurve.FormatHex(curve.P),
            ["ProductKeys:A"] = ProductKeyCurve.FormatHex(curve.A),
            ["ProductKeys:B"] = ProductKeyCurve.FormatHex(curve.B),
            ["ProductKeys:GeneratorX"] = ProductKeyCurve.FormatHex(generator.AffineXCoord.ToBigInteger()),
            ["ProductKeys:GeneratorY"] = ProductKeyCurve.FormatHex(generator.AffineYCoord.ToBigInteger()),
            ["ProductKeys:PublicKeyX"] = ProductKeyCurve.FormatHex(publicKey.AffineXCoord.ToBigInteger()),
            ["ProductKeys:PublicKeyY"] = ProductKeyCurve.FormatHex(publicKey.AffineYCoord.ToBigInteger()),
        };

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void SignedKeyIsValidWithItsSerial()
    {
        var validator = new ProductKeyActivationCodeValidator(CreateConfiguration(SigningKey.Curve));
        var productKey = new ProductKeySigner(SigningKey).CreateProductKey(7, 123);

        var check = validator.CheckActivationCode(productKey);

        Assert.True(check.IsValid);
        Assert.Equal(ProductKeyFields.ComposeSerial(7, 123), check.Serial);
    }

    [Theory]
    [InlineData("NOPVK-NOPVK-NOPVK-NOPVK-NOPVK")]
    [InlineData("AAAAA-AAAAA-AAAAA-AAAAA-AAAAA")]
    [InlineData("FCKGW-RHQQ2-YXRKT-8TG6W-2B7Q8")]
    public void OtherCodesAreInvalid(string activationCode)
    {
        var validator = new ProductKeyActivationCodeValidator(CreateConfiguration(SigningKey.Curve));

        var check = validator.CheckActivationCode(activationCode);

        Assert.False(check.IsValid);
        Assert.Null(check.Serial);
    }

    [Fact]
    public void MissingCurveValueFailsLoudly()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ProductKeys:P"] = "17" })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new ProductKeyActivationCodeValidator(configuration));
    }
}
