using System.Buffers.Binary;
using System.Text;

namespace ReLiveWP.Services.Login.Utilities;

public static class AuthIdentity
{
    public const string MicrosoftAccountProvider = "MicrosoftAccount";

    private const uint IdentityVersion2 = 0x201;
    private const int IdentityHeaderLength = 0x30;
    private const int CredentialsHeaderLength = 0x1C;

    private const uint IdentityFlagsReserved = 0x10000;
    private const uint IdentityFlagsIdProvider = 0x80000;

    private const int IdentityTrailerLength = 8;

    private static readonly byte[] PasswordCredType =
        new Guid("28bfc32f-10f6-4738-98d1-1ac061df716a").ToByteArray();

    public static string PackPassword(string username, string password)
        => Convert.ToBase64String(PackPassword(username, MicrosoftAccountProvider, password));

    public static byte[] PackPassword(string username, string provider, string password)
    {
        var userBytes = Encoding.Unicode.GetBytes(username);
        var providerBytes = Encoding.Unicode.GetBytes(provider);
        var passwordBytes = Encoding.Unicode.GetBytes(password);

        var userOffset = IdentityHeaderLength;
        var providerOffset = userOffset + userBytes.Length;
        var credentialsOffset = providerOffset + providerBytes.Length;

        // the host encrypts the password region in 8 byte blocks and bounds-checks the rounded up
        // length against the packed credentials, so reserve it here
        var passwordSpan = (passwordBytes.Length + 7) & ~7;
        var credentialsLength = CredentialsHeaderLength + passwordSpan;
        var totalLength = credentialsOffset + credentialsLength;

        var buffer = new byte[totalLength + IdentityTrailerLength];
        var identity = buffer.AsSpan();

        BinaryPrimitives.WriteUInt32LittleEndian(identity[0x00..], IdentityVersion2);
        BinaryPrimitives.WriteUInt16LittleEndian(identity[0x04..], IdentityHeaderLength);
        BinaryPrimitives.WriteUInt32LittleEndian(identity[0x08..], (uint)totalLength);
        BinaryPrimitives.WriteUInt32LittleEndian(identity[0x0C..], (uint)userOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(identity[0x10..], (ushort)userBytes.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(identity[0x14..], (uint)providerOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(identity[0x18..], (ushort)providerBytes.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(identity[0x1C..], (uint)credentialsOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(identity[0x20..], (ushort)credentialsLength);
        BinaryPrimitives.WriteUInt32LittleEndian(identity[0x24..], IdentityFlagsReserved | IdentityFlagsIdProvider);

        userBytes.CopyTo(identity[userOffset..]);
        providerBytes.CopyTo(identity[providerOffset..]);

        var credentials = identity[credentialsOffset..];
        BinaryPrimitives.WriteUInt16LittleEndian(credentials[0x00..], CredentialsHeaderLength);
        BinaryPrimitives.WriteUInt16LittleEndian(credentials[0x02..], (ushort)credentialsLength);
        PasswordCredType.CopyTo(credentials[0x04..]);
        BinaryPrimitives.WriteUInt32LittleEndian(credentials[0x14..], CredentialsHeaderLength);
        BinaryPrimitives.WriteUInt16LittleEndian(credentials[0x18..], (ushort)passwordBytes.Length);
        passwordBytes.CopyTo(credentials[CredentialsHeaderLength..]);

        return buffer;
    }
}
