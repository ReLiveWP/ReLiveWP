using System.Text;
using ReLiveWP.Backend.Skybox.Commands;

namespace ReLiveWP.Backend.Skybox.Services;

public class DeviceCommandService(IHttpClientFactory httpClientFactory, ILogger<DeviceCommandService> logger)
{
    public async Task SendAsync(string channelUrl, DeviceCommand command, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(channelUrl);

        var body = Convert.ToBase64String(command.Serialize());
        using var request = new HttpRequestMessage(HttpMethod.Post, channelUrl)
        {
            Content = new StringContent(body, Encoding.ASCII, "text/plain")
        };

        request.Headers.TryAddWithoutValidation("X-WindowsPhone-Target", "raw");
        request.Headers.TryAddWithoutValidation("X-NotificationClass", "3");

        using var client = httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("sent {Action} (req {RequestId}) to {ChannelUrl}: {Status}",
            command.Action, command.RequestId, channelUrl, (int)response.StatusCode);
    }
}
