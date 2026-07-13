using System.Globalization;

namespace ReLiveWP.Services.Login.Models.Ppsecure;

public record GetKeyDataModel(string KeyMaterial, long TimeStamp, string Purpose = "StrongCredentialKey");

public record PpsecureFaultModel(string ErrorCode, uint ErrorSubcode = 0)
{
    public string ServerTime => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
