using System.Globalization;

namespace ReLiveWP.Services.Login.Models.DeviceCredential;

public record DeviceAddResponseModel(string PuidHex, string ErrorCode = "")
{
    public string ServerTime => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
}