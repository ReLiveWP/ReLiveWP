using System.Globalization;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneNumbers;
using ReLiveWP.Identity;
using ReLiveWP.Services.Devices.Models;
using ReLiveWP.Services.Devices.Services;
using ReLiveWP.Services.Grpc.DeviceRegistration;
using ReLiveWP.Services.Grpc.FindMyPhone;

namespace ReLiveWP.Services.Devices.Controllers;

[ApiController]
[Route("[controller]/[action]/{id?}")]
public class DevicesController(
    ILogger<DevicesController> logger,
    ICarrierLookupService carrierLookupService,
    FindMyPhone.FindMyPhoneClient skyboxClient,
    DeviceRegistration.DeviceRegistrationClient deviceRegistrationClient) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ActionName("@me")]
    public async IAsyncEnumerable<ConnectedDeviceModel> GetDevicesAsync()
    {
        if (User == null)
            throw new UnauthorizedAccessException();

        var devices = skyboxClient.GetDevicesForUser(new GetDevicesForUserRequest() { UserId = User.Id() });

        await foreach (var device in devices.ResponseStream.ReadAllAsync())
        {
            var locale = new CultureInfo(device.HasLcid ? device.Lcid : 0x0409);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(device.Timezone);
            var carrierInfo = carrierLookupService.GetCarrierInfo(device.Operator);


            Device? registeredDevice = null;
            try
            {
                registeredDevice = await deviceRegistrationClient.DeviceByIdAsync(new DeviceByIdRequest() { DeviceId = device.UniqueId });
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex, "Missing device registration for {DeviceId}", device.UniqueId);
            }

            var phoneNumber = "None";
            if (device.HasPhoneNumber && !string.IsNullOrWhiteSpace(phoneNumber))
            {
                phoneNumber = device.PhoneNumber;

                try
                {
                    var phoneNumberUtil = PhoneNumberUtil.GetInstance();
                    var number = phoneNumberUtil.Parse(phoneNumber, carrierInfo?.CountryCode ?? "ZZ");
                    phoneNumber = phoneNumberUtil.Format(number, PhoneNumberFormat.INTERNATIONAL);
                }
                catch { }
            }

            var carrier = device.Operator;
            if (carrierInfo?.Brand != null)
            {
                carrier = carrierInfo.Value.Brand;
                if (carrierInfo?.CountryCode != null)
                    carrier += $" ({carrierInfo.Value.CountryCode})";
            }
            else if (carrierInfo?.Operator != null)
            {
                carrier = carrierInfo.Value.Operator;
            }

            yield return new ConnectedDeviceModel(
                device.UniqueId,
                device.FriendlyName,
                device.Manufacturer,
                device.Model,
                carrier,
                phoneNumber,
                registeredDevice?.Imei,
                device.OsVersion,
                locale.DisplayName,
                timeZone.DisplayName,
                device.HasColourTheme ? device.ColourTheme : 1,
                device.HasAccentColour ? device.AccentColour : null);
        }
    }

    [HttpGet]
    [Authorize]
    [ActionName("info")]
    public async Task<ConnectedDeviceExtendedModel> GetDeviceExtendedInfoAsync(string id)
    {
        if (User == null)
            throw new UnauthorizedAccessException();

        var device = await skyboxClient.GetDeviceExtendedInfoAsync(new GetDeviceExtendedInfoRequest() { UserId = User.Id(), DeviceGuid = id });

        var locale = new CultureInfo(device.HasLcid ? device.Lcid : 0x0409);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(device.Timezone);
        var carrierInfo = carrierLookupService.GetCarrierInfo(device.Operator);


        Device? registeredDevice = null;
        try
        {
            registeredDevice = await deviceRegistrationClient.DeviceByIdAsync(new DeviceByIdRequest() { DeviceId = device.UniqueId });
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Missing device registration for {DeviceId}", device.UniqueId);
        }

        var phoneNumber = "None";
        if (device.HasPhoneNumber && !string.IsNullOrWhiteSpace(phoneNumber))
        {
            phoneNumber = device.PhoneNumber;

            try
            {
                var phoneNumberUtil = PhoneNumberUtil.GetInstance();
                var number = phoneNumberUtil.Parse(phoneNumber, carrierInfo?.CountryCode ?? "ZZ");
                phoneNumber = phoneNumberUtil.Format(number, PhoneNumberFormat.INTERNATIONAL);
            }
            catch { }
        }

        var carrier = device.Operator;
        if (carrierInfo?.Brand != null)
        {
            carrier = carrierInfo.Value.Brand;
            if (carrierInfo?.CountryCode != null)
                carrier += $" ({carrierInfo.Value.CountryCode})";
        }
        else if (carrierInfo?.Operator != null)
        {
            carrier = carrierInfo.Value.Operator;
        }

        return new ConnectedDeviceExtendedModel(
            device.UniqueId,
            device.Manufacturer,
            device.Model,
            carrier,
            phoneNumber,
            device.OsVersion,
            device.FriendlyName,
            device.HasColourTheme ? device.ColourTheme : 1,
            device.HasAccentColour ? device.AccentColour : null,
            locale.DisplayName,
            timeZone.DisplayName,
            device.HasBatteryLevel ? device.BatteryLevel : 0,
            device.HasStorageRemaining ? device.StorageRemaining : null,
            device.HasPinLocked && device.PinLocked,
            device.HasSimLocked && device.SimLocked,
            device.HasWorkingSet ? device.WorkingSet : 0,
            device.LastSeen?.ToDateTimeOffset(),
            device.HasLastSeenLat ? device.LastSeenLat : null,
            device.HasLastSeenLong ? device.LastSeenLong : null,
            registeredDevice?.Imei
        );
    }

    [HttpPut]
    [Authorize]
    [ActionName("ping")]
    public async Task<IActionResult> PingDeviceAsync(string id)
    {
        if (User == null)
            return Unauthorized(); // should never happen

        await skyboxClient.SendDeviceCommandAsync(new DeviceCommandRequest() { UserId = User.Id(), DeviceGuid = id, Command = DeviceCommandRequestType.CommandRing });

        return Accepted();
    }
}
