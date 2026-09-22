using Org.BouncyCastle.Asn1.X509;
using ReLiveWP.Services.Activation.Utilities;

namespace ReLiveWP.Services.Activation.Tests;

public class ActivationCodeSubjectTests
{
    // taken from a real WP7 device registration that typed this code in OOBE
    private const string RealDeviceCode = "AAAAA-AAAAA-AAAAA-AAAAA-AAAAA";
    private const string RealDeviceCommonName = "urn:wp-ac-hash:EPYYPJFFhKDGwoIzwFKGi_NU3d5w0Myps-5eQB8Z-O0";

    [Fact]
    public void CommonNameMatchesRealDevice()
    {
        Assert.Equal(RealDeviceCommonName, ActivationCodeSubject.CreateCommonName(RealDeviceCode));
    }

    [Fact]
    public void MatchingSubjectIsAccepted()
    {
        var subject = new X509Name($"CN={RealDeviceCommonName}");

        Assert.True(ActivationCodeSubject.MatchesActivationCode(subject, RealDeviceCode));
    }

    [Theory]
    [InlineData("BBBBB-BBBBB-BBBBB-BBBBB-BBBBB")]
    [InlineData("aaaaa-aaaaa-aaaaa-aaaaa-aaaaa")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("")]
    public void OtherCodesAreRejected(string activationCode)
    {
        var subject = new X509Name($"CN={RealDeviceCommonName}");

        Assert.False(ActivationCodeSubject.MatchesActivationCode(subject, activationCode));
    }

    [Fact]
    public void SubjectWithoutCommonNameIsRejected()
    {
        var subject = new X509Name("O=ReLiveWP");

        Assert.False(ActivationCodeSubject.MatchesActivationCode(subject, RealDeviceCode));
    }
}
