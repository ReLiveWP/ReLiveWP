using System.Text;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Push.Data;
using ReLiveWP.Services.Push.Nsp;

namespace ReLiveWP.Services.Push.Controllers;

[ApiController]
[Route("/{controller}/{id?}")]
public class ChannelController(
    ChannelStore channelStore,
    PushPresence pushPresence,
    PresenceDirectory presence,
    PushRouter router,
    PushInstance instance,
    NotificationQueue notificationQueue,
    ILogger<ChannelController> logger) : ControllerBase
{
    private static readonly string[] ForwardHeaders = [
        "X-WindowsPhone-Target",
        "X-NotificationClass",
        "X-MessageID"
    ];

    [HttpPost]
    public async Task<IActionResult> Post(string id, CancellationToken ct)
    {
        var channel = await channelStore.FindByTokenAsync(id, ct);
        if (channel == null)
        {
            logger.LogWarning("notification for unknown channel token {Token}", id);
            return NotFound();
        }

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);

        var payload = BuildMessage(Request, ms.ToArray());
        var notificationClass = ParseClass(Request.Headers["X-NotificationClass"]);

        // connected to this instance right now? straight down the wire
        if (pushPresence.TryGet(channel.DeviceId, out var session)
            && session.TrySend((uint)channel.ChannelId, notificationClass, payload))
        {
            logger.LogInformation("delivered {Class} ({Len}B) to {DeviceId} channel {Id}",
                notificationClass, payload.Length, channel.DeviceId, channel.ChannelId);

            return Ok();
        }

        // connected to another instance? publish to the instance that holds the socket
        var owner = await presence.GetOwnerAsync(channel.DeviceId);
        if (owner != null && owner != instance.Id)
        {
            await router.PublishAsync(owner, new RoutedNotification(
                channel.DeviceId, (uint)channel.ChannelId, (uint)notificationClass, payload));

            logger.LogInformation("routed {Class} ({Len}B) to {DeviceId} on {Owner} channel {Id}",
                notificationClass, payload.Length, channel.DeviceId, owner, channel.ChannelId);

            return Accepted();
        }

        // nobody holds it, queue until the device reconnects
        logger.LogInformation("{DeviceId} offline, queued {Class} ({Len}B) for channel {Id}",
            channel.DeviceId, notificationClass, payload.Length, channel.ChannelId);

        await notificationQueue.EnqueueAsync(channel.DeviceId, channel.ChannelId, payload, (uint)notificationClass, ct: ct);
        return Accepted();
    }

    private static byte[] BuildMessage(HttpRequest request, byte[] body)
    {
        var headers = new StringBuilder();
        foreach (var name in ForwardHeaders)
        {
            var value = request.Headers[name].ToString();
            if (!string.IsNullOrEmpty(value))
                headers.Append(name).Append(':').Append(value).Append("\r\n");
        }

        // this may be an error condition but who are we to judge
        if (headers.Length == 0)
            return body;

        headers.Append("\r\n");
        var head = Encoding.ASCII.GetBytes(headers.ToString());

        var message = new byte[head.Length + body.Length];
        Buffer.BlockCopy(head, 0, message, 0, head.Length);
        Buffer.BlockCopy(body, 0, message, head.Length, body.Length);
        return message;
    }

    private static NspNotificationClass ParseClass(string header) =>
        uint.TryParse(header, out var value) && Enum.IsDefined(typeof(NspNotificationClass), value)
            ? (NspNotificationClass)value
            : NspNotificationClass.Toast;
}
