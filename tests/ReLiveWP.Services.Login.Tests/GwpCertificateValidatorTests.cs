using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Login.Services;

namespace ReLiveWP.Services.Login.Tests;

public class GwpCertificateValidatorTests : IDisposable
{
    private const string Wp7UsageOid = "1.3.6.1.4.1.311.71.1.1";
    private const string ActivationCodeHash = "EPYYPJFFhKDGwoIzwFKGi_NU3d5w0Myps-5eQB8Z-O0";

    private static readonly DateTimeOffset AuthorityNotBefore = DateTimeOffset.UtcNow.AddYears(-5);
    private static readonly DateTimeOffset AuthorityNotAfter = DateTimeOffset.UtcNow.AddYears(5);

    private readonly string _bundlePath = Path.GetTempFileName();
    private readonly X509Certificate2 _root;
    private readonly X509Certificate2 _provisioningCa;

    public GwpCertificateValidatorTests()
    {
        _root = CreateAuthority("CN=Test Root", null);
        _provisioningCa = CreateAuthority("CN=Test Device Provisioning PCA", _root);

        File.WriteAllText(_bundlePath, _provisioningCa.ExportCertificatePem() + "\n" + _root.ExportCertificatePem());
    }

    public void Dispose()
    {
        File.Delete(_bundlePath);
        GC.SuppressFinalize(this);
    }

    private GwpCertificateValidator CreateValidator(string? bundlePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Signup:ProvisioningCACertFile"] = bundlePath })
            .Build();

        return new GwpCertificateValidator(configuration, NullLogger<GwpCertificateValidator>.Instance);
    }

    private static X509Certificate2 CreateAuthority(string subject, X509Certificate2? issuer)
    {
        var key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));

        var notBefore = AuthorityNotBefore;
        var notAfter = AuthorityNotAfter;

        if (issuer == null)
            return request.CreateSelfSigned(notBefore, notAfter);

        using var signed = request.Create(issuer, notBefore, notAfter, RandomNumberGenerator.GetBytes(8));
        return signed.CopyWithPrivateKey(key);
    }

    private static X509Certificate2 CreateDeviceCertificate(
        X509Certificate2 issuer,
        string subject = "CN=urn:wp-ac-hash:" + ActivationCodeHash,
        string usageOid = Wp7UsageOid,
        bool expired = false)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.2"), new Oid(usageOid)], true));

        var notBefore = expired ? DateTimeOffset.UtcNow.AddYears(-3) : DateTimeOffset.UtcNow.AddDays(-7);
        var notAfter = expired ? DateTimeOffset.UtcNow.AddYears(-2) : DateTimeOffset.UtcNow.AddYears(1);

        var generator = new Sha1RsaSignatureGenerator(issuer.GetRSAPrivateKey()!);
        return request.Create(issuer.SubjectName, generator, notBefore, notAfter, RandomNumberGenerator.GetBytes(8));
    }

    // WindowsPhoneCertificateService signs protocol 1.0 requests SHA1withRSA, which CertificateRequest refuses to produce
    private sealed class Sha1RsaSignatureGenerator(RSA issuerKey) : X509SignatureGenerator
    {
        private static readonly byte[] Sha1WithRsaAlgorithmIdentifier =
            [0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x05, 0x05, 0x00];

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm) => Sha1WithRsaAlgorithmIdentifier;

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
            => issuerKey.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

        protected override PublicKey BuildPublicKey() => PublicKey.CreateFromSubjectPublicKeyInfo(issuerKey.ExportSubjectPublicKeyInfo(), out _);
    }

    [Fact]
    public void Device_certificate_from_the_provisioning_ca_is_genuine()
    {
        var check = CreateValidator(_bundlePath).CheckCertificate(CreateDeviceCertificate(_provisioningCa));

        Assert.True(check.IsGenuine, check.FailureReason);
        Assert.Equal(ActivationCodeHash, check.ActivationCodeHash);
    }

    [Fact]
    public void Expired_device_certificate_is_still_genuine()
    {
        var check = CreateValidator(_bundlePath).CheckCertificate(CreateDeviceCertificate(_provisioningCa, expired: true));

        Assert.True(check.IsGenuine, check.FailureReason);
    }

    [Fact]
    public void Certificate_from_another_ca_is_refused()
    {
        var strangerCa = CreateAuthority("CN=Test Device Provisioning PCA", null);

        var check = CreateValidator(_bundlePath).CheckCertificate(CreateDeviceCertificate(strangerCa));

        Assert.False(check.IsGenuine);
    }

    [Fact]
    public void Certificate_without_the_gwp_usage_is_refused()
    {
        var check = CreateValidator(_bundlePath).CheckCertificate(CreateDeviceCertificate(_provisioningCa, usageOid: "1.3.6.1.5.5.7.3.1"));

        Assert.False(check.IsGenuine);
    }

    [Fact]
    public void Certificate_with_another_subject_is_refused()
    {
        var check = CreateValidator(_bundlePath).CheckCertificate(CreateDeviceCertificate(_provisioningCa, subject: "CN=0123456789ABCDEF.devicedns.live.com"));

        Assert.False(check.IsGenuine);
    }

    [Fact]
    public void Nothing_is_genuine_without_a_configured_ca()
    {
        var check = CreateValidator(null).CheckCertificate(CreateDeviceCertificate(_provisioningCa));

        Assert.False(check.IsGenuine);
    }

    [Fact]
    public void Certificate_arrives_through_the_nginx_escaped_header()
    {
        var certificate = CreateDeviceCertificate(_provisioningCa);
        var context = new DefaultHttpContext();
        context.Request.Headers[GwpCertificateValidator.ClientCertificateHeader] = Uri.EscapeDataString(certificate.ExportCertificatePem());

        var check = CreateValidator(_bundlePath).CheckRequestCertificate(context.Request);

        Assert.True(check.IsGenuine, check.FailureReason);
        Assert.Equal(certificate.Thumbprint, check.Certificate!.Thumbprint);
    }

    [Fact]
    public void Missing_or_broken_header_is_refused()
    {
        var validator = CreateValidator(_bundlePath);
        var context = new DefaultHttpContext();

        Assert.False(validator.CheckRequestCertificate(context.Request).IsGenuine);

        context.Request.Headers[GwpCertificateValidator.ClientCertificateHeader] = "-----BEGIN%20CERTIFICATE-----%0Anope";
        Assert.False(validator.CheckRequestCertificate(context.Request).IsGenuine);
    }
}
