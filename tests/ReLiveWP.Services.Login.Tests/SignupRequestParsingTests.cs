using System.Text;
using Microsoft.AspNetCore.Http;
using ReLiveWP.Services.Login.Utilities;

namespace ReLiveWP.Services.Login.Tests;

// request shapes are the format strings in LiveIDUtils.dll
public class SignupRequestParsingTests
{
    private const string AvailabilityRequest =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"><soap:Body><CheckSigninNameAvailability xmlns=\"http://schemas.microsoft.com/Passport/Mobile/WebServices/Signup/V1\"><Version>1.0</Version><SigninName>someone@relivewp.net</SigninName><NeedSuggestedName>true</NeedSuggestedName><FirstName></FirstName><LastName></LastName></CheckSigninNameAvailability></soap:Body></soap:Envelope>";

    private static string CreateRequest(string memberName, string alternateEmail = "backup@example.com")
        => "<p:userData xmlns:p=\"http://schemas.microsoft.com/Passport/User\">"
         + $"<p:credential type=\"Web\"><p:property name=\"Name\">{memberName}</p:property><p:property name=\"WinLiveUser\">true</p:property>"
         + $"<p:property name=\"AlternateEmail\">{alternateEmail}</p:property>"
         + "<p:EncryptedProperties><p:x509SKI type=\"HexBinary\">ABCDEF</p:x509SKI><p:CipherValue>AAAA</p:CipherValue><p:Version>1</p:Version></p:EncryptedProperties>"
         + "</p:credential><p:profile><p:propertyCollection name=\"Authorization_CS\"><p:property name=\"WinLiveTOUVersion\">5</p:property><p:property name=\"F2_Hotmail\">1</p:property></p:propertyCollection></p:profile></p:userData>";

    [Fact]
    public void Signin_name_comes_out_of_the_device_request()
    {
        Assert.Equal("someone@relivewp.net", SignupServiceSoap.ReadSigninName(AvailabilityRequest));
    }

    [Fact]
    public void Signin_name_is_null_for_other_operations()
    {
        Assert.Null(SignupServiceSoap.ReadSigninName("<Envelope><Body><Something/></Body></Envelope>"));
        Assert.Null(SignupServiceSoap.ReadSigninName("not xml"));
    }

    [Fact]
    public void User_data_comes_out_of_the_device_request()
    {
        var userData = EasyIdUserDataParser.ParseUserData(CreateRequest("someone@relivewp.net"));

        Assert.NotNull(userData);
        Assert.Equal("someone@relivewp.net", userData.MemberName);
        Assert.Equal("backup@example.com", userData.AlternateEmail);
        Assert.Equal("ABCDEF", userData.Ski);
        Assert.Equal("AAAA", userData.CipherValue);
        Assert.Equal("1", userData.Version);
    }

    [Fact]
    public void Unescaped_member_name_is_refused_rather_than_misread()
    {
        Assert.Null(EasyIdUserDataParser.ParseUserData(CreateRequest("a&b@relivewp.net")));
        Assert.Null(EasyIdUserDataParser.ParseUserData(CreateRequest("a<b@relivewp.net")));
    }

    [Fact]
    public void User_data_without_a_cipher_value_is_refused()
    {
        var withoutCipher = CreateRequest("someone@relivewp.net").Replace("<p:CipherValue>AAAA</p:CipherValue>", "");

        Assert.Null(EasyIdUserDataParser.ParseUserData(withoutCipher));
    }

    [Fact]
    public async Task Trailing_nuls_are_trimmed_from_the_body()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(AvailabilityRequest + "\0\0"));

        var body = await DeviceRequestBody.ReadTrimmedBodyAsync(context.Request);

        Assert.Equal(AvailabilityRequest, body);
    }
}
