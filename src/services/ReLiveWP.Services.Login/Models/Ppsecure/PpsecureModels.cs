using System.Globalization;

namespace ReLiveWP.Services.Login.Models.Ppsecure;

public record GetKeyDataPurpose(string Purpose, string KeyMaterial, string TimeStamp);

public record GetKeyDataModel(IReadOnlyList<GetKeyDataPurpose> Purposes);

public record PpsecureFaultModel(string ErrorCode, uint ErrorSubcode = 0)
{
    public string ServerTime => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
