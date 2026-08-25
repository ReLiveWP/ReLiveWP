using System.Buffers.Binary;
using System.Text;
using ReLiveWP.Services.Login.Utilities;

namespace ReLiveWP.Services.Login.Tests;

// mirrors LiveValidateAuthIdEx2 / LiveGetAuthDataFromIdentityEx2 in livessp.dll
public class AuthIdentityTests
{
    private const string Username = "someone@relivewp.net";
    private const string Password = "correct horse battery staple";

    private static readonly Guid PasswordCredType = new("28bfc32f-10f6-4738-98d1-1ac061df716a");

    private static byte[] Pack() => AuthIdentity.PackPassword(Username, AuthIdentity.MicrosoftAccountProvider, Password);

    private static uint ReadU32(byte[] buffer, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset));
    private static ushort ReadU16(byte[] buffer, int offset) => BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset));

    [Fact]
    public void Version_is_the_only_one_livessp_accepts()
    {
        Assert.Equal(0x201u, ReadU32(Pack(), 0x00));
    }

    [Fact]
    public void Structure_length_leaves_the_eight_byte_trailer_unmarshal_demands()
    {
        var buffer = Pack();
        var structureLength = ReadU32(buffer, 0x08);

        Assert.True(structureLength <= buffer.Length - 8);
        Assert.True(ReadU16(buffer, 0x04) >= 0x30);
        Assert.True(structureLength >= ReadU16(buffer, 0x04));
    }

    [Theory]
    [InlineData(0x0C, 0x10)]
    [InlineData(0x14, 0x18)]
    [InlineData(0x1C, 0x20)]
    public void Every_section_passes_the_offset_and_length_bounds_checks(int offsetField, int lengthField)
    {
        var buffer = Pack();
        var headerLength = ReadU16(buffer, 0x04);
        var structureLength = ReadU32(buffer, 0x08);

        var offset = ReadU32(buffer, offsetField);
        var length = ReadU16(buffer, lengthField);

        if (offset != 0)
            Assert.True(offset >= headerLength);

        Assert.True(offset <= structureLength);
        Assert.True(length < structureLength);
        Assert.True(structureLength - length >= offset);
    }

    [Fact]
    public void User_and_domain_lengths_are_even_so_the_utf16_copy_is_accepted()
    {
        var buffer = Pack();

        Assert.Equal(0, ReadU16(buffer, 0x10) % 2);
        Assert.Equal(0, ReadU16(buffer, 0x18) % 2);
    }

    [Fact]
    public void Flags_opt_the_identity_into_sspi_encryption()
    {
        var flags = ReadU32(Pack(), 0x24);

        Assert.Equal(0x10000u, flags & 0x10000u);
        Assert.Equal(0x80000u, flags & 0x80000u);
        Assert.Equal(0u, flags & 0x60u);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Password1")]
    [InlineData("eight!!!")]
    [InlineData("a rather longer passphrase than usual")]
    public void Password_region_leaves_room_for_the_hosts_eight_byte_rounding(string password)
    {
        var buffer = AuthIdentity.PackPassword(Username, AuthIdentity.MicrosoftAccountProvider, password);

        var credentials = (int)ReadU32(buffer, 0x1C);
        var credentialsLength = ReadU16(buffer, 0x20);
        var structureLength = ReadU16(buffer, credentials + 0x02);

        var dataOffset = ReadU32(buffer, credentials + 0x14);
        var dataLength = ReadU16(buffer, credentials + 0x18);
        var rounded = (dataLength + 7) & ~7;

        Assert.True(dataOffset + rounded <= structureLength);
        Assert.True(structureLength <= credentialsLength);
        Assert.True(credentials + dataOffset + rounded <= buffer.Length);
    }

    [Fact]
    public void Package_list_is_absent()
    {
        var buffer = Pack();

        Assert.Equal(0u, ReadU32(buffer, 0x28));
        Assert.Equal(0, ReadU16(buffer, 0x2C));
    }

    [Fact]
    public void Packed_credentials_satisfy_the_nested_header_checks()
    {
        var buffer = Pack();
        var offset = (int)ReadU32(buffer, 0x1C);
        var length = ReadU16(buffer, 0x20);

        Assert.NotEqual(0, length);
        Assert.True(length >= 0x1C);

        var credentialsHeader = ReadU16(buffer, offset + 0x00);
        var credentialsLength = ReadU16(buffer, offset + 0x02);

        Assert.True(credentialsHeader >= 0x1C);
        Assert.True(credentialsLength <= length);
        Assert.True(credentialsHeader <= credentialsLength);

        var dataOffset = ReadU32(buffer, offset + 0x14);
        var dataLength = ReadU16(buffer, offset + 0x18);

        Assert.True(dataOffset + dataLength >= dataOffset);
        Assert.True(dataOffset + dataLength <= credentialsLength);
        Assert.Equal(0, dataLength % 2);
    }

    [Fact]
    public void Credential_type_is_the_password_guid()
    {
        var buffer = Pack();
        var offset = (int)ReadU32(buffer, 0x1C);

        Assert.Equal(PasswordCredType, new Guid(buffer.AsSpan(offset + 0x04, 16)));
    }

    [Fact]
    public void Domain_is_the_provider_name_livessp_compares_against()
    {
        var buffer = Pack();
        var domain = Encoding.Unicode.GetString(buffer, (int)ReadU32(buffer, 0x14), ReadU16(buffer, 0x18));

        Assert.Equal("MicrosoftAccount", domain);
    }

    [Fact]
    public void Unpacking_the_way_livessp_does_recovers_the_credentials()
    {
        var buffer = Pack();

        var user = Encoding.Unicode.GetString(buffer, (int)ReadU32(buffer, 0x0C), ReadU16(buffer, 0x10));

        var credentials = (int)ReadU32(buffer, 0x1C);
        var passwordOffset = credentials + (int)ReadU32(buffer, credentials + 0x14);
        var password = Encoding.Unicode.GetString(buffer, passwordOffset, ReadU16(buffer, credentials + 0x18));

        Assert.Equal(Username, user);
        Assert.Equal(Password, password);
    }

    [Fact]
    public void Encoded_buffer_fits_both_host_limits()
    {
        var encoded = AuthIdentity.PackPassword(Username, Password);

        Assert.True(encoded.Length <= 5000);
        Assert.True(Convert.FromBase64String(encoded).Length <= 0x7FFF);
    }
}
