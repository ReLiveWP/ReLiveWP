using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ReLiveWP.Services.Login.Services;

public record GwpCertificateCheck(
    bool IsGenuine,
    string? ActivationCodeHash,
    X509Certificate2? Certificate,
    string? FailureReason);

public class GwpCertificateValidator
{
    public const string ClientCertificateHeader = "X-ReLive-Client-Cert";

    private const string ActivationCodeSubjectPrefix = "urn:wp-ac-hash:";
    private static readonly string[] GenuineUsageOids = ["1.3.6.1.4.1.311.71.1.1", "1.3.6.1.4.1.311.71.1.2"];

    private readonly X509Certificate2Collection _trustAnchors = [];
    private readonly X509Certificate2Collection _intermediates = [];

    public GwpCertificateValidator(IConfiguration configuration, ILogger<GwpCertificateValidator> logger)
    {
        var bundlePath = configuration["Signup:ProvisioningCACertFile"];
        if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath))
        {
            logger.LogWarning("Signup:ProvisioningCACertFile is not configured, every GWP certificate will be refused");
            return;
        }

        var bundle = new X509Certificate2Collection();
        bundle.ImportFromPemFile(bundlePath);

        foreach (var certificate in bundle)
        {
            var isSelfSigned = certificate.SubjectName.RawData.AsSpan().SequenceEqual(certificate.IssuerName.RawData);
            (isSelfSigned ? _trustAnchors : _intermediates).Add(certificate);
        }
    }

    public GwpCertificateCheck CheckRequestCertificate(HttpRequest request)
    {
        var header = request.Headers[ClientCertificateHeader].ToString();
        if (string.IsNullOrEmpty(header))
            return Refuse("no client certificate");

        X509Certificate2 certificate;
        try
        {
            certificate = X509Certificate2.CreateFromPem(Uri.UnescapeDataString(header));
        }
        catch (CryptographicException)
        {
            return Refuse("client certificate header did not parse");
        }

        return CheckCertificate(certificate);
    }

    public GwpCertificateCheck CheckCertificate(X509Certificate2 certificate)
    {
        if (_trustAnchors.Count == 0)
            return Refuse("no provisioning CA configured", certificate);

        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.AddRange(_trustAnchors);
        chain.ChainPolicy.ExtraStore.AddRange(_intermediates);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid;

        if (!chain.Build(certificate))
        {
            var statuses = string.Join(", ", chain.ChainStatus.Select(status => status.Status));
            return Refuse($"chain did not build: {statuses}", certificate);
        }

        if (!HasGenuineUsage(certificate))
            return Refuse("no GWP enhanced key usage", certificate);

        var subject = certificate.GetNameInfo(X509NameType.SimpleName, false);
        if (!subject.StartsWith(ActivationCodeSubjectPrefix, StringComparison.Ordinal)
            || subject.Length == ActivationCodeSubjectPrefix.Length)
            return Refuse($"unexpected subject {subject}", certificate);

        var activationCodeHash = subject[ActivationCodeSubjectPrefix.Length..];
        return new GwpCertificateCheck(true, activationCodeHash, certificate, null);
    }

    private static bool HasGenuineUsage(X509Certificate2 certificate)
    {
        var usages = certificate.Extensions
            .OfType<X509EnhancedKeyUsageExtension>()
            .SelectMany(extension => extension.EnhancedKeyUsages.Cast<Oid>())
            .Select(oid => oid.Value);

        return usages.Any(oid => GenuineUsageOids.Contains(oid));
    }

    private static GwpCertificateCheck Refuse(string reason, X509Certificate2? certificate = null)
        => new(false, null, certificate, reason);
}
