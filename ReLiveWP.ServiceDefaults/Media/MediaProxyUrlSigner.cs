using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using ReLiveWP.ServiceDefaults.Outbound;

namespace ReLiveWP.ServiceDefaults.Media;

public sealed partial class MediaProxyUrlSigner
{
    public const string KeysSection = "Media:ProxyKeys";
    public const string PublicRootKey = "PublicUrls:Media";
    public const string FileExtension = ".jpg";

    public const int MinimumKeyBytes = 32;
    public const int SignatureBytes = 16;
    public const int MaxEncodedSourceLength = 4096;

    private readonly Dictionary<string, byte[]> keys;
    private readonly string? signingVersion;
    private readonly string? publicRoot;

    public MediaProxyUrlSigner(IReadOnlyDictionary<string, string> secrets, Uri? publicRoot)
    {
        keys = [];

        foreach (var (version, secret) in secrets)
        {
            if (string.IsNullOrWhiteSpace(secret))
                continue;

            if (!TryParseVersion(version, out _))
                throw new InvalidOperationException($"{KeysSection}:{version} is not named v1, v2 and so on.");

            var key = Encoding.UTF8.GetBytes(secret.Trim());
            if (key.Length < MinimumKeyBytes)
                throw new InvalidOperationException($"{KeysSection}:{version} is shorter than {MinimumKeyBytes} bytes.");

            keys[version] = key;
        }

        signingVersion = keys.Keys
            .OrderByDescending(version => TryParseVersion(version, out var number) ? number : 0)
            .FirstOrDefault();

        this.publicRoot = publicRoot?.AbsoluteUri.TrimEnd('/');
    }

    public static MediaProxyUrlSigner FromConfiguration(IConfiguration configuration)
    {
        var secrets = configuration.GetSection(KeysSection)
            .GetChildren()
            .Where(child => child.Value != null)
            .ToDictionary(child => child.Key, child => child.Value!);

        var root = configuration[PublicRootKey];
        var publicRoot = Uri.TryCreate(root, UriKind.Absolute, out var parsed) ? parsed : null;

        return new MediaProxyUrlSigner(secrets, publicRoot);
    }

    public bool CanVerify => keys.Count > 0;

    public string? SignUrl(Uri source, MediaSize size)
    {
        if (publicRoot == null)
            return null;

        var path = SignPath(source, size);
        return path == null ? null : $"{publicRoot}{path}";
    }

    public string? SignPath(Uri source, MediaSize size)
    {
        if (signingVersion == null)
            return null;

        if (!ExternalRequestGuard.IsAcceptableUri(source))
            return null;

        var sourceUrl = source.AbsoluteUri;
        var encodedSource = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(sourceUrl));
        if (encodedSource.Length > MaxEncodedSourceLength)
            return null;

        var sizeName = MediaSizes.FormatSize(size);
        var signature = ComputeSignature(keys[signingVersion], signingVersion, sizeName, sourceUrl);
        var encodedSignature = WebEncoders.Base64UrlEncode(signature);

        return $"/{signingVersion}/{sizeName}/{encodedSignature}/{encodedSource}{FileExtension}";
    }

    public string SignUrlOrOriginal(string url, MediaSize size)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var source))
            return url;

        return SignUrl(source, size) ?? url;
    }

    public bool TryVerifyPath(string version, string sizeName, string signature, string encodedSource,
                              out Uri source, out MediaSize size)
    {
        source = null!;
        size = default;

        if (!keys.TryGetValue(version, out var key))
            return false;

        if (!MediaSizes.TryParseSize(sizeName, out var parsedSize))
            return false;

        if (encodedSource.Length > MaxEncodedSourceLength)
            return false;

        if (!TryDecodeBase64Url(signature, out var presentedSignature) || presentedSignature.Length != SignatureBytes)
            return false;

        if (!TryDecodeBase64Url(encodedSource, out var sourceBytes))
            return false;

        string sourceUrl;
        try
        {
            sourceUrl = new UTF8Encoding(false, true).GetString(sourceBytes);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        var expected = ComputeSignature(key, version, sizeName, sourceUrl);
        if (!CryptographicOperations.FixedTimeEquals(expected, presentedSignature))
            return false;

        if (!ExternalRequestGuard.TryParseAcceptableUri(sourceUrl, out var parsedSource))
            return false;

        source = parsedSource;
        size = parsedSize.Value;
        return true;
    }

    private static byte[] ComputeSignature(byte[] key, string version, string sizeName, string sourceUrl)
    {
        var payload = Encoding.UTF8.GetBytes($"{version}\n{sizeName}\n{sourceUrl}");
        return HMACSHA256.HashData(key, payload)[..SignatureBytes];
    }

    private static bool TryDecodeBase64Url(string value, out byte[] bytes)
    {
        try
        {
            bytes = WebEncoders.Base64UrlDecode(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private static bool TryParseVersion(string version, out int number)
    {
        number = 0;

        var match = VersionPattern().Match(version);
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }

    [GeneratedRegex("^v([0-9]{1,6})$")]
    private static partial Regex VersionPattern();
}
