using System.Globalization;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhoneNumbers;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.FindMyPhone;
using ReLiveWP.Services.Login.Models;

namespace ReLiveWP.Services.Login.Controllers;

[Route("devices/[action]/{id?}")]
[ApiController]
public class DeviceController(FindMyPhone.FindMyPhoneClient findMyPhoneClient) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ActionName("@me")]
    public async IAsyncEnumerable<ConnectedDeviceModel> GetDevicesAsync()
    {
        if (User == null)
            throw new UnauthorizedAccessException();

        var devices = findMyPhoneClient.GetDevicesForUser(new GetDevicesForUserRequest() { UserId = User.Id() });

        await foreach (var device in devices.ResponseStream.ReadAllAsync())
        {
            var locale = new CultureInfo(int.Parse(device.Locale));

            var phoneNumber = "None";
            if (device.HasPhoneNumber && !string.IsNullOrWhiteSpace(phoneNumber))
            {
                phoneNumber = device.PhoneNumber;
                if (!phoneNumber.StartsWith('+') && !phoneNumber.StartsWith('0'))
                    phoneNumber = "+" + phoneNumber;

                try
                {
                    var phoneNumberUtil = PhoneNumberUtil.GetInstance();
                    var number = phoneNumberUtil.Parse(phoneNumber, "ZZ");
                    phoneNumber = phoneNumberUtil.Format(number, PhoneNumberFormat.INTERNATIONAL);
                }
                catch { }
            }

            yield return new ConnectedDeviceModel(device.FriendlyName, device.Manufacturer, device.Model, device.Operator, phoneNumber, device.OsVersion, locale.Name);
        }
    }
}
