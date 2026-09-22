using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.X509;

namespace ReLiveWP.Services.Activation.Utilities;

public static class ActivationCodeSubject
{
    private const string CommonNamePrefix = "urn:wp-ac-hash:";

    public static string CreateCommonName(string activationCode)
    {
        var codeBytes = Encoding.UTF8.GetBytes(activationCode);
        var codeHash = SHA256.HashData(codeBytes);
        return CommonNamePrefix + Base64Url.EncodeToString(codeHash);
    }

    public static bool MatchesActivationCode(X509Name subject, string activationCode)
    {
        var commonNames = subject.GetValueList(X509Name.CN);
        if (commonNames.Count != 1)
            return false;

        var expectedCommonName = CreateCommonName(activationCode);
        return string.Equals(commonNames[0] as string, expectedCommonName, StringComparison.Ordinal);
    }
}
