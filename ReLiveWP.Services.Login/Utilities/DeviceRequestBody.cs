using System.Text;

namespace ReLiveWP.Services.Login.Utilities;

public static class DeviceRequestBody
{
    public static async Task<string> ReadTrimmedBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        return body.TrimEnd('\0');
    }
}
