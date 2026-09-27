using Org.BouncyCastle.Math;

namespace ReLiveWP.ProductKeys.Tests;

// XPKeygen's default "Windows XP VLK" preset: resources/bink/BINKXP.bin, preset index 2
public class XpCurveOracleTests
{
    private static readonly ProductKeyCurve XpVlkCurve = ProductKeyCurve.FromHex(
        "92DDCF14CB9E71F4489A2E9BA350AE29454D98CB93BDBCC07D62B502EA12238EE904A8B20D017197AAE0C103B32713A9",
        "1",
        "0",
        "46E3775ECE21B0898D39BEA57050D422A0AF989E497962BAEE2CB17E0A28D5360D5476B8DC966443E37A14F1AEF37742",
        "7C8E741D2C34F4478E325469CD491603D807222C9C4AC09DDB2B31B3CE3F7CC191B3580079932BC6BEF70BE27604F65E",
        "5D8DBE75198015EC41C45AAB6143542EB098F6A5CC9CE4178A1B8A1E7ABBB5BC64DF64FAF6177DC1B0988AB00BA94BF8",
        "23A2909A0B4803C89F910C7191758B48746CEA4D5FF07667444ACDB9512080DBCA55E6EBF30433672B894F44ACE92BFA");

    private static readonly ProductKeySigningKey XpVlkSigningKey = new(
        XpVlkCurve,
        new BigInteger("61760995553426173"),
        new BigInteger("24306963676698312"));

    [Fact]
    public void StoredPublicKeyIsNegatedPrivateKeyTimesGenerator()
    {
        XpVlkSigningKey.EnsureConsistent();
    }

    [Fact]
    public void RealXpVlkKeyVerifies()
    {
        var verifier = new ProductKeyVerifier(XpVlkCurve);

        Assert.True(verifier.TryVerifyProductKey("FCKGW-RHQQ2-YXRKT-8TG6W-2B7Q8", out var fields));
        Assert.Equal(640, fields.ChannelId);
        Assert.Equal(35, fields.Sequence);
        Assert.False(fields.IsUpgrade);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(640, 123456, false)]
    [InlineData(640, 123456, true)]
    [InlineData(999, 999999, true)]
    public void SignedKeysVerify(int channelId, int sequence, bool isUpgrade)
    {
        var signer = new ProductKeySigner(XpVlkSigningKey);
        var verifier = new ProductKeyVerifier(XpVlkCurve);

        for (var i = 0; i < 20; i++)
        {
            var productKey = signer.CreateProductKey(channelId, sequence, isUpgrade);

            Assert.True(verifier.TryVerifyProductKey(productKey, out var fields), productKey);
            Assert.Equal(channelId, fields.ChannelId);
            Assert.Equal(sequence, fields.Sequence);
            Assert.Equal(isUpgrade, fields.IsUpgrade);
        }
    }
}
