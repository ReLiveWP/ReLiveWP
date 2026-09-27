using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneNumbers;
using ReLiveWP.Identity;
using ReLiveWP.Services.Devices.Models;
using ReLiveWP.Services.Devices.Services;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.DeviceRegistration;
using ReLiveWP.Services.Grpc.FindMyPhone;

namespace ReLiveWP.Services.Devices.Controllers;

[ApiController]
[Route("[controller]/[action]/{id?}")]
public class DevicesController(
    ILogger<DevicesController> logger,
    ICarrierLookupService carrierLookupService,
    IWebHostEnvironment environment,
    FindMyPhone.FindMyPhoneClient skyboxClient,
    DeviceRegistration.DeviceRegistrationClient deviceRegistrationClient,
    Authentication.AuthenticationClient authenticationClient) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ActionName("@me")]
    public async IAsyncEnumerable<ConnectedDeviceModel> GetDevicesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (User == null)
            throw new UnauthorizedAccessException();

        var devices = skyboxClient.GetDevicesForUser(new GetDevicesForUserRequest() { UserId = User.Id() }, cancellationToken: cancellationToken);

        await foreach (var device in devices.ResponseStream.ReadAllAsync(cancellationToken))
        {
            var carrierInfo = carrierLookupService.GetCarrierInfo(device.Operator);

            var registeredDevice = await FindRegistrationAsync(device.UniqueId, cancellationToken);

            yield return new ConnectedDeviceModel(
                device.UniqueId,
                device.FriendlyName,
                device.Manufacturer,
                device.Model,
                ResolveCarrier(device.Operator, carrierInfo),
                ResolvePhoneNumber(device.HasPhoneNumber, device.PhoneNumber, carrierInfo),
                registeredDevice?.Imei,
                device.OsVersion,
                SafeLocaleDisplay(device.HasLcid, device.Lcid),
                SafeTimeZoneDisplay(device.Timezone),
                device.HasColourTheme ? device.ColourTheme : 1,
                device.HasAccentColour ? device.AccentColour : null);
        }
    }

    [HttpGet]
    [Authorize]
    [ActionName("info")]
    public async Task<ConnectedDeviceExtendedModel> GetDeviceExtendedInfoAsync(string id, CancellationToken cancellationToken)
    {
        if (User == null)
            throw new UnauthorizedAccessException();

        var device = await skyboxClient.GetDeviceExtendedInfoAsync(new GetDeviceExtendedInfoRequest() { UserId = User.Id(), DeviceGuid = id }, cancellationToken: cancellationToken);

        var carrierInfo = carrierLookupService.GetCarrierInfo(device.Operator);

        var registeredDevice = await FindRegistrationAsync(device.UniqueId, cancellationToken);

        return new ConnectedDeviceExtendedModel(
            device.UniqueId,
            device.Manufacturer,
            device.Model,
            ResolveCarrier(device.Operator, carrierInfo),
            ResolvePhoneNumber(device.HasPhoneNumber, device.PhoneNumber, carrierInfo),
            device.OsVersion,
            device.FriendlyName,
            device.HasColourTheme ? device.ColourTheme : 1,
            device.HasAccentColour ? device.AccentColour : null,
            SafeLocaleDisplay(device.HasLcid, device.Lcid),
            SafeTimeZoneDisplay(device.Timezone),
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

    // a device that never activated has no registration, that is normal and just means no IMEI.
    // anything else is DeviceRegistration being down and should not look the same
    private async Task<Device?> FindRegistrationAsync(string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            return await deviceRegistrationClient.DeviceByIdAsync(new DeviceByIdRequest() { DeviceId = deviceId }, cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            logger.LogInformation("No device registration for {DeviceId}", deviceId);
            return null;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "Device registration lookup for {DeviceId} failed", deviceId);
            DevicesMetrics.RegistrationLookupFailures.Add(1);
            return null;
        }
    }

    [HttpGet("~/[controller]/image/{size}/{device}")]
    [AllowAnonymous]
    public IActionResult GetDeviceImage(string size, string device)
    {
        var imageName = DeviceImageResolver.Resolve(device);
        var suffix = string.Equals(size, "small", StringComparison.OrdinalIgnoreCase) ? "-small" : "";
        var path = Path.Combine(environment.WebRootPath, "devices", $"{imageName}{suffix}.png");

        if (!System.IO.File.Exists(path))
            return NotFound();

        Response.Headers.CacheControl = "public, max-age=2592000, immutable";
        return PhysicalFile(path, "image/png");
    }

    [HttpPut]
    [Authorize]
    [ActionName("ping")]
    public async Task<IActionResult> PingDeviceAsync(string id, CancellationToken cancellationToken)
    {
        if (User == null)
            return Unauthorized(); // should never happen

        var response = await skyboxClient.SendDeviceCommandAsync(new DeviceCommandRequest() { UserId = User.Id(), DeviceGuid = id, Command = DeviceCommandRequestType.CommandRing }, cancellationToken: cancellationToken);
        DevicesMetrics.RecordCommandRequested("ring");

        return Accepted(new { requestId = response.RequestId });
    }


    [HttpPut]
    [Authorize]
    [ActionName("locate")]
    public async Task<IActionResult> LocateDeviceAsync(string id, CancellationToken cancellationToken)
    {
        if (User == null)
            return Unauthorized(); // should never happen

        var response = await skyboxClient.SendDeviceCommandAsync(new DeviceCommandRequest() { UserId = User.Id(), DeviceGuid = id, Command = DeviceCommandRequestType.CommandLocate }, cancellationToken: cancellationToken);
        DevicesMetrics.RecordCommandRequested("locate");

        return Accepted(new { requestId = response.RequestId });
    }

    [HttpDelete]
    [Authorize]
    [ActionName("remove")]
    public async Task<IActionResult> RemoveDeviceAsync(string id, CancellationToken cancellationToken)
    {
        if (User == null)
            return Unauthorized(); // should never happen

        try
        {
            await skyboxClient.RemoveDeviceAsync(new RemoveDeviceRequest() { UserId = User.Id(), DeviceGuid = id }, cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            return NotFound();
        }

        try
        {
            await authenticationClient.RevokeDeviceTokensAsync(new RevokeDeviceTokensRequest() { DeviceId = id }, cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "Failed to revoke tokens for removed device {DeviceId}", id);
        }

        return NoContent();
    }

    [HttpGet]
    [Authorize]
    [ActionName("events")]
    [DisableHttpMetrics]
    public IResult EventsStream(string id, CancellationToken cancellationToken)
    {
        if (User == null)
            return Results.Unauthorized();

        var call = skyboxClient.StreamCommandStatus(
            new StreamCommandStatusRequest() { UserId = User.Id(), DeviceGuid = id },
            cancellationToken: cancellationToken);

        return TypedResults.ServerSentEvents(MapStatusAsync(call, cancellationToken), eventType: "status");
    }

    private static async IAsyncEnumerable<CommandStatusModel> MapStatusAsync(
        AsyncServerStreamingCall<CommandStatusUpdate> call,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        DevicesMetrics.EventsStreamActive.Add(1);
        try
        {
            using (call)
            {
                // equivalent to, except catches RpcException for cancellation
                // await foreach (var update in call.ResponseStream.ReadAllAsync(cancellationToken))
                await using var enumerator = call.ResponseStream.ReadAllAsync(cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);
                while (!cancellationToken.IsCancellationRequested)
                {
                    CommandStatusUpdate? update = null;
                    try
                    {
                        if (!await enumerator.MoveNextAsync())
                            yield break;

                        update = enumerator.Current;
                    }
                    catch (RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.Cancelled)
                    {
                        yield break;
                    }

                    yield return new CommandStatusModel(
                        update.RequestId,
                        update.Action switch
                        {
                            DeviceCommandRequestType.CommandRing => "ring",
                            DeviceCommandRequestType.CommandLocate => "locate",
                            _ => "unknown"
                        },
                        update.Result,
                        update.Final,
                        string.IsNullOrEmpty(update.Data) ? null : update.Data,
                        update.Reported?.ToDateTimeOffset(),
                        update.HasLat ? update.Lat : null,
                        update.HasLong ? update.Long : null,
                        update.HasAccuracy ? update.Accuracy : null);

                }
            }
        }
        finally
        {
            DevicesMetrics.EventsStreamActive.Add(-1);
            DevicesMetrics.RecordEventsStreamEnd(started, cancellationToken.IsCancellationRequested);
        }
    }

    private static string SafeLocaleDisplay(bool hasLcid, int lcid)
    {
        try
        {
            return new CultureInfo(hasLcid ? lcid : 0x0409).DisplayName;
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo(0x0409).DisplayName;
        }
    }

    private static string SafeTimeZoneDisplay(string timezone)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezone).DisplayName;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return timezone;
        }
    }

    private static string ResolvePhoneNumber(bool hasPhoneNumber, string phoneNumber, CarrierInfo? carrierInfo)
    {
        if (!hasPhoneNumber || string.IsNullOrWhiteSpace(phoneNumber))
            return "None";

        try
        {
            var phoneNumberUtil = PhoneNumberUtil.GetInstance();
            var number = phoneNumberUtil.Parse(phoneNumber, carrierInfo?.CountryCode ?? "ZZ");
            return phoneNumberUtil.Format(number, PhoneNumberFormat.INTERNATIONAL);
        }
        catch
        {
            return phoneNumber;
        }
    }

    private static string ResolveCarrier(string @operator, CarrierInfo? carrierInfo)
    {
        if (carrierInfo?.Brand != null)
        {
            var carrier = carrierInfo.Value.Brand;
            if (carrierInfo.Value.CountryCode != null)
                carrier += $" ({carrierInfo.Value.CountryCode})";
            return carrier;
        }

        if (carrierInfo?.Operator != null)
            return carrierInfo.Value.Operator;

        return @operator;
    }
}
