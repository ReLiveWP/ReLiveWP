using System.Globalization;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Login.Models;
using static ReLiveWP.Services.Grpc.User;

namespace ReLiveWP.Services.Login.Controllers;

[Route("device/[action]/{id?}")]
[ApiController]
public class DeviceController(DeviceRegistration.DeviceRegistrationClient deviceRegistrationClient) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ActionName("@me")]
    public async IAsyncEnumerable<ConnectedDeviceModel> GetDevicesAsync()
    {
        if (User == null)
            throw new UnauthorizedAccessException();

        var devices = deviceRegistrationClient.DevicesForUser(new DevicesForUserRequest() { UserId = User.Id() });

        await foreach (var device in devices.ResponseStream.ReadAllAsync())
        {
            var locale = new CultureInfo(int.Parse(device.Locale));
            yield return new ConnectedDeviceModel(device.Manufacturer, device.Model, device.Operator, device.Imei, device.OsVersion, locale.Name);
        }
    }
}
