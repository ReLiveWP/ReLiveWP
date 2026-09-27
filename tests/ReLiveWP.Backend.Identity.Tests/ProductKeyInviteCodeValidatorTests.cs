using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Security;
using ReLiveWP.Backend.Identity.Services;
using ReLiveWP.ProductKeys;

namespace ReLiveWP.Backend.Identity.Tests;

public class ProductKeyInviteCodeValidatorTests
{
    private static readonly ProductKeySigningKey InviteSigningKey =
        ProductKeyCurveGenerator.GenerateSigningKey(new SecureRandom());

    private static readonly ProductKeySigningKey DeviceSigningKey =
        ProductKeyCurveGenerator.GenerateSigningKey(new SecureRandom());

    private static IConfiguration CreateConfiguration(ProductKeyCurve curve)
    {
        var generator = curve.Generator.Normalize();
        var publicKey = curve.PublicKey.Normalize();

        var values = new Dictionary<string, string?>
        {
            ["InviteKeys:Required"] = "true",
            ["InviteKeys:P"] = ProductKeyCurve.FormatHex(curve.P),
            ["InviteKeys:A"] = ProductKeyCurve.FormatHex(curve.A),
            ["InviteKeys:B"] = ProductKeyCurve.FormatHex(curve.B),
            ["InviteKeys:GeneratorX"] = ProductKeyCurve.FormatHex(generator.AffineXCoord.ToBigInteger()),
            ["InviteKeys:GeneratorY"] = ProductKeyCurve.FormatHex(generator.AffineYCoord.ToBigInteger()),
            ["InviteKeys:PublicKeyX"] = ProductKeyCurve.FormatHex(publicKey.AffineXCoord.ToBigInteger()),
            ["InviteKeys:PublicKeyY"] = ProductKeyCurve.FormatHex(publicKey.AffineYCoord.ToBigInteger()),
        };

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static ProductKeyInviteCodeValidator CreateValidator()
        => new(CreateConfiguration(InviteSigningKey.Curve));

    [Fact]
    public void Signed_invite_is_valid_with_its_serial()
    {
        var inviteCode = new ProductKeySigner(InviteSigningKey).CreateProductKey(3, 456);

        var check = CreateValidator().CheckInviteCode(inviteCode);

        Assert.True(check.IsValid);
        Assert.Equal(ProductKeyFields.ComposeSerial(3, 456), check.Serial);
    }

    [Fact]
    public void Lowercase_invite_is_still_valid()
    {
        var inviteCode = new ProductKeySigner(InviteSigningKey).CreateProductKey(3, 457);

        var check = CreateValidator().CheckInviteCode(inviteCode.ToLowerInvariant());

        Assert.True(check.IsValid);
    }

    [Fact]
    public void Key_from_another_curve_is_not_an_invite()
    {
        var deviceKey = new ProductKeySigner(DeviceSigningKey).CreateProductKey(3, 456);

        var check = CreateValidator().CheckInviteCode(deviceKey);

        Assert.False(check.IsValid);
        Assert.Null(check.Serial);
    }

    [Theory]
    [InlineData("")]
    [InlineData("FCKGW-RHQQ2-YXRKT-8TG6W-2B7Q8")]
    public void Other_codes_are_invalid(string inviteCode)
    {
        var check = CreateValidator().CheckInviteCode(inviteCode);

        Assert.False(check.IsValid);
    }

    [Fact]
    public void Missing_curve_value_fails_loudly()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["InviteKeys:Required"] = "true" })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new ProductKeyInviteCodeValidator(configuration));
    }
}
