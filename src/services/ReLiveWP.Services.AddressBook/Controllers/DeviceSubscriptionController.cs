using System.Xml.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.AddressBook.Models;

namespace ReLiveWP.Services.AddressBook.Controllers;

[Controller]
[Authorize]
public class DeviceSubscriptionController(ILogger<DeviceSubscriptionController> logger) : ControllerBase
{
    private static readonly XmlSerializer Serializer = new(typeof(DeviceSubscriptionInfoList));

    [HttpPut]
    [Route("/abservice/devicesubscription.aspx")]
    public async Task<IActionResult> Put([FromQuery] string deviceid, [FromQuery] string devicetype)
    {
        using var reader = new StreamReader(Request.Body);
        var xml = (await reader.ReadToEndAsync(HttpContext.RequestAborted)).TrimStart();

        DeviceSubscriptionInfoList list;
        try
        {
            list = (DeviceSubscriptionInfoList)Serializer.Deserialize(new StringReader(xml))!;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Malformed DeviceSubscriptionInfoList from device {Device}", deviceid);
            return BadRequest();
        }

        foreach (var sub in list.Subscriptions)
        {
            var detail = (sub.SubscriptionDetails as ContactSubscriptionDetails)?.Detail?.Detail;
            var contacts = detail?.ContactHandles
                .Select(h => h.SourceHandle)
                .Where(s => s is not null)
                .Select(s => $"{s!.SourceId}:{s.ObjectId}") ?? [];

            logger.LogInformation(
                "Device subscription: user={User} device={Device}/{Type} view={View} expiry={Expiry}s channel={Channel} contacts=[{Contacts}]",
                User.Id(), deviceid, devicetype, sub.View, sub.RequestedExpirationPeriodInSeconds,
                detail?.ChannelUri, string.Join(", ", contacts));
        }

        return Ok();
    }
}
