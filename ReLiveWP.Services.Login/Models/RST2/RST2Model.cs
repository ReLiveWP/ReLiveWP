using System.Globalization;

namespace ReLiveWP.Services.Login.Models.RST2;

public class RST2Model
{
    public string TimeZ { get; set; } = "";
    public string TomorrowZ { get; set; } = "";
    public string Time5MZ { get; set; } = "";
    public string PUIDHex { get; set; } = "";
    public string CID { get; set; } = "";

    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public string IP { get; set; } = "";

    public string Token { get; set; } = "";

    // Encrypted Passport.NET/tb key
    public string CipherValue { get; set; } = "";
    public string ProofToken { get; set; } = "";
    public string DaTokenReference { get; set; } = "";

    public RST2Token[] Tokens { get; set; } = [];
}

public class RST2Token
{
    // wsu:Id emitted on the BinarySecurityToken; unique within the response.
    public string Id { get; set; } = "";

    public string Domain { get; set; } = "";
    public string Token { get; set; } = "";
    public string BinarySecret { get; set; } = "";

    public string Type { get; set; } = "";
    public string? ProofToken { get; set; } 

    public string Reference { get; set; } = "";

    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Expires { get; set; }

    public string CreatedZ => Created.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
    public string ExpiresZ => Expires.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
