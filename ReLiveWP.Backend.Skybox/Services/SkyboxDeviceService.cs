using System.Globalization;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Org.BouncyCastle.Ocsp;
using ReLiveWP.Backend.Skybox.Commands;
using ReLiveWP.Backend.Skybox.Data;
using ReLiveWP.Services.Grpc.Email;
using ReLiveWP.Services.Grpc.FindMyPhone;

namespace ReLiveWP.Backend.Skybox.Services;

public class SkyboxDeviceService(
    ILogger<SkyboxDeviceService> logger,
    SkyDbContext dbContext,
    DeviceCommandService deviceCommand,
    CommandRegistry registry,
    CommandStatusHub statusHub,
    Email.EmailClient email) : FindMyPhone.FindMyPhoneBase
{
    private const string SERVICE_NAME = "find my phone";

    public override async Task<RegisterDeviceResponse> RegisterDevice(RegisterDeviceRequest request, ServerCallContext context)
    {
        var userId = new Guid(request.UserId);
        var deviceGuid = request.DeviceGuid.ToLowerInvariant();
        var existing = await dbContext.Devices.FirstOrDefaultAsync(d => d.DeviceGuid == deviceGuid);
        if (existing != null)
        {
            dbContext.Devices.Remove(existing);
        }

        var device = new SkyDevice()
        {
            OwnerId = userId,

            // Correlates to UniqueId in DeviceRegistration
            DeviceGuid = deviceGuid
        };

        SkyProfileKeys.ApplyProperties(device, request.DeviceProps);

        dbContext.Devices.Add(device);
        await dbContext.SaveChangesAsync();

        try
        {
            await email.SendSystemEmailAsync(new SendSystemEmailRequest()
            {
                UserId = userId.ToString(),
                ServiceName = SERVICE_NAME,
                Subject = "New device added!",
                Body = $"A new device was added to your account!\r\nThe device \"{device.FriendlyName}\" has been registered and can now be managed at https://relivewp.net/my/device/{device.DeviceGuid}\r\nIf you don't recognise this device, please reset your password and contact an administrator immediately."
            });
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "Failed to send notification email for new device registration.");
        }

        return new RegisterDeviceResponse() { Code = 0, Enabled = true, Message = "OK" };
    }

    public override async Task<RegisterChannelResponse> RegisterChannel(RegisterChannelRequest request, ServerCallContext context)
    {
        var deviceGuid = request.DeviceGuid.ToLowerInvariant();
        var device = await dbContext.Devices.AsTracking().FirstOrDefaultAsync(d => d.DeviceGuid == deviceGuid)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Device not found!"));

        var valid = Uri.TryCreate(request.NotificationUri, UriKind.Absolute, out var uri)
            && (uri.Host == "push.relivewp.net" || uri.Host == "push.int.relivewp.net")
            && uri.Scheme == Uri.UriSchemeHttps;

        device.NotificationChannelUrl = valid ? request.NotificationUri : null;

        dbContext.Update(device);
        await dbContext.SaveChangesAsync();

        // Logged after the save so it reflects what actually committed, not just intent.
        if (valid)
            logger.LogInformation("Device {DeviceId} has valid notification channel URL: {Url}", device.DeviceGuid, request.NotificationUri);
        else
            logger.LogWarning("Device {DeviceId} has invalid notification channel URL: {Url}", device.DeviceGuid, request.NotificationUri);

        return new RegisterChannelResponse() { Code = 0, Enabled = true, Message = "OK" };
    }

    public override async Task<UpdateDeviceInfoResponse> UpdateDeviceInfo(UpdateDeviceInfoRequest request, ServerCallContext context)
    {
        var deviceGuid = request.DeviceGuid.ToLowerInvariant();
        var device = await dbContext.Devices.AsTracking().FirstOrDefaultAsync(d => d.DeviceGuid == deviceGuid)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Device not found!"));

        SkyProfileKeys.ApplyProperties(device, request.DeviceProps);

        dbContext.Update(device);

        await dbContext.SaveChangesAsync();

        return new UpdateDeviceInfoResponse() { Code = 0, Enabled = true, Message = "OK" };
    }

    public override async Task GetDevicesForUser(GetDevicesForUserRequest request, IServerStreamWriter<UserDevice> responseStream, ServerCallContext context)
    {
        var ownerId = Guid.Parse(request.UserId);
        foreach (var device in dbContext.Devices.Where(d => d.OwnerId == ownerId))
        {
            var resp = new UserDevice()
            {
                Manufacturer = device.Make,
                Model = device.Model,
                OsVersion = device.OSVersion,
                FriendlyName = device.FriendlyName,
                Locale = device.LCID.ToString(),
                UniqueId = device.DeviceGuid,
                ColourTheme = device.ColorTheme,
                AccentColour = "#" + (device.ColorAccent & 0x00FFFFFF).ToString("x6"),

                Lcid = device.LCID,
                Timezone = device.TZ,
                //BatteryLevel = device.BatteryLevel,
                //StorageRemaining = device.StorageRemaining,
                //PinLocked = device.PinLocked,
                //SimLocked = device.SimLocked
            };

            if (!string.IsNullOrEmpty(device.MobileOperator))
                resp.Operator = device.MobileOperator;

            if (!string.IsNullOrWhiteSpace(device.PhoneNumber))
                resp.PhoneNumber = device.PhoneNumber;

            await responseStream.WriteAsync(resp);
        }
    }

    public override async Task<UserDeviceExtended> GetDeviceExtendedInfo(GetDeviceExtendedInfoRequest request, ServerCallContext context)
    {
        var ownerId = Guid.Parse(request.UserId);
        var deviceGuid = request.DeviceGuid.ToLowerInvariant();
        var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.DeviceGuid == deviceGuid && d.OwnerId == ownerId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Device not found!"));

        var resp = new UserDeviceExtended()
        {
            Manufacturer = device.Make,
            Model = device.Model,
            OsVersion = device.OSVersion,
            FriendlyName = device.FriendlyName,
            UniqueId = device.DeviceGuid,
            ColourTheme = device.ColorTheme,
            AccentColour = "#" + (device.ColorAccent & 0x00FFFFFF).ToString("x6"),

            Lcid = device.LCID,
            Timezone = device.TZ,
            BatteryLevel = device.BatteryLevel,
            StorageRemaining = device.StorageRemaining,
            PinLocked = device.PinLocked,
            SimLocked = device.SimLocked,
            WorkingSet = device.MaxWorkingSet
        };

        if (!string.IsNullOrEmpty(device.MobileOperator))
            resp.Operator = device.MobileOperator;

        if (!string.IsNullOrWhiteSpace(device.PhoneNumber))
            resp.PhoneNumber = device.PhoneNumber;

        if (device.LastLocation != null)
        {
            resp.LastSeen = device.LastLocation.Reported.ToTimestamp();
            resp.LastSeenLat = device.LastLocation.Latitude;
            resp.LastSeenLong = device.LastLocation.Longitude;
        }

        return resp;
    }

    public override async Task<DeviceCommandResponse> SendDeviceCommand(DeviceCommandRequest request, ServerCallContext context)
    {
        var ownerId = Guid.Parse(request.UserId);
        var deviceId = request.DeviceGuid.ToLowerInvariant();
        var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.DeviceGuid == deviceId && d.OwnerId == ownerId)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Device not found!"));

        if (string.IsNullOrWhiteSpace(device.NotificationChannelUrl))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Device {deviceId} notification channel not registered."));

        var command = request.Command switch
        {
            DeviceCommandRequestType.CommandRing => DeviceCommand.Ring(),
            DeviceCommandRequestType.CommandLocate => DeviceCommand.Locate(),
            _ => throw new RpcException(new Status(StatusCode.InvalidArgument, "Unsupported command."))
        };

        var record = await registry.CreateAsync(device.DeviceGuid, ownerId, command.Action);
        command.RequestId = record.RequestId;

        await deviceCommand.SendAsync(device.NotificationChannelUrl, command);

        // TODO: other requests
        if (request.Command == DeviceCommandRequestType.CommandRing)
        {
            try
            {
                await email.SendSystemEmailAsync(new SendSystemEmailRequest()
                {
                    UserId = request.UserId,
                    ServiceName = SERVICE_NAME,
                    Subject = $"A sound was played on {device.FriendlyName}",
                    Body = $"A sound was played on {device.FriendlyName} at {DateTime.Now:T} on {DateTime.Now:D}\r\n\r\nIf you don't recognise this action, please reset your password and contact an administrator immediately."
                });
            }
            catch (RpcException ex)
            {
                logger.LogError(ex, "Failed to send notification email for new device registration.");
            }
        }

        return new DeviceCommandResponse() { RequestId = record.RequestId, IsQueued = false };
    }

    public override async Task<ReportCommandStatusResponse> ReportCommandStatus(ReportCommandStatusRequest request, ServerCallContext context)
    {
        var deviceGuid = request.DeviceGuid.ToLowerInvariant();
        var record = await registry.GetAsync(deviceGuid, request.RequestId);
        if (record == null)
        {
            logger.LogWarning("Status for unknown command {DeviceId}/{RequestId}", deviceGuid, request.RequestId);
            return new ReportCommandStatusResponse() { Code = 1 };
        }

        var evt = new CommandStatusEvent(record.RequestId, record.Action, request.Result, request.Final,
            request.Data, DateTimeOffset.UtcNow, null, null, null);

        if (record.Action == DeviceCommandAction.Locate && !string.IsNullOrWhiteSpace(request.Data))
        {
            try
            {
                var location = SkyDeviceLocation.Parse(request.Data);
                evt = evt with { Reported = location.Reported, Lat = location.Latitude, Long = location.Longitude, Accuracy = location.Altitude };

                var device = await dbContext.Devices.AsTracking().FirstOrDefaultAsync(d => d.DeviceGuid == deviceGuid);
                if (device != null)
                {
                    device.LastLocation = location;
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Couldn't parse locate data '{Data}'", request.Data);
            }
        }

        await statusHub.PublishAsync(deviceGuid, evt);
        await registry.SetStateAsync(record, request.Final ? CommandState.Final : CommandState.Active);

        return new ReportCommandStatusResponse() { Code = 0 };
    }

    public override async Task StreamCommandStatus(StreamCommandStatusRequest request, IServerStreamWriter<CommandStatusUpdate> responseStream, ServerCallContext context)
    {
        var ownerId = Guid.Parse(request.UserId);
        var deviceGuid = request.DeviceGuid.ToLowerInvariant();
        if (!await dbContext.Devices.AnyAsync(d => d.DeviceGuid == deviceGuid && d.OwnerId == ownerId))
            throw new RpcException(new Status(StatusCode.NotFound, "Device not found!"));

        try
        {
            await foreach (var evt in statusHub.StreamAsync(deviceGuid, context.CancellationToken))
            {
                var update = new CommandStatusUpdate()
                {
                    RequestId = evt.RequestId,
                    Action = evt.Action == DeviceCommandAction.Locate ? DeviceCommandRequestType.CommandLocate : DeviceCommandRequestType.CommandRing,
                    Result = evt.Result,
                    Final = evt.Final,
                    Data = evt.Data ?? "",
                    Reported = evt.Reported.ToTimestamp(),
                };

                if (evt.Lat is double lat) update.Lat = lat;
                if (evt.Long is double lng) update.Long = lng;
                if (evt.Accuracy is double acc) update.Accuracy = acc;

                await responseStream.WriteAsync(update);
            }
        }
        catch (OperationCanceledException)
        {
            // shut uppppppp
        }
    }

    public override async Task<ReportDeviceStatusResponse> ReportDeviceStatus(ReportDeviceStatusRequest request, ServerCallContext context)
    {
        var deviceGuid = request.DeviceGuid.ToLowerInvariant();
        var device = await dbContext.Devices.AsTracking().FirstOrDefaultAsync(d => d.DeviceGuid == deviceGuid);
        if (device == null)
            return new ReportDeviceStatusResponse() { Code = 1 };

        if (request.HasBatteryLevel)
            device.BatteryLevel = request.BatteryLevel;
        if (request.HasClientVersion)
            device.ClientVersion = request.ClientVersion;
        if (request.DeviceTime != null)
            device.LastSeen = request.DeviceTime.ToDateTimeOffset();

        await dbContext.SaveChangesAsync();
        return new ReportDeviceStatusResponse() { Code = 0 };
    }

    private static class SkyProfileKeys
    {
        public const string Make = "SkyProfile.Make";
        public const string Model = "SkyProfile.Model";
        public const string OSVersion = "SkyProfile.OSVersion";
        public const string ClientVersion = "SkyProfile.ClientVersion";
        public const string Capabilities = "SkyProfile.Capabilities";
        public const string Locale = "SkyProfile.Locale";
        public const string TimezoneName = "SkyProfile.TimezoneName";
        public const string FriendlyName = "SkyProfile.FriendlyName";
        public const string ColorTheme = "SkyProfile.ColorTheme";
        public const string ColorAccent = "SkyProfile.ColorAccent";
        public const string PhoneNumber = "SkyProfile.PhoneNumber";
        public const string MobileOperator = "SkyProfile.MobileOperator";
        public const string CommercializedMobileOperator = "SkyProfile.CommercializedMobileOperator";
        public const string Imsi = "SkyProfile.Imsi";
        public const string SimIdHash = "SkyProfile.SimIdHash";
        public const string MaxWorkingSet = "SkyProfile.MaxWorkingSet";
        public const string BatteryLevel = "SkyProfile.BatteryLevel";
        public const string PinLockEnabled = "SkyProfile.PinLockEnabled";
        public const string SimLockEnabled = "SkyProfile.SimLockEnabled";
        public const string StorageRemaining = "SkyProfile.StorageRemaining";
        public const string ScreenResolution = "SkyProfile.ScreenResolution";

        public const string LastLocation = "SkyTrigger.LastLocation";

        public static void ApplyProperties(SkyDevice device, IDictionary<string, string> props)
        {
            if (props.TryGetValue(Make, out var make))
                device.Make = make;
            if (props.TryGetValue(Model, out var model))
                device.Model = model;
            if (props.TryGetValue(OSVersion, out var osVersion))
                device.OSVersion = osVersion;
            if (props.TryGetValue(ClientVersion, out var clientVersion))
                device.ClientVersion = clientVersion;
            if (props.TryGetValue(Capabilities, out var caps))
                device.Capabilities = Convert.ToUInt32(caps, 16);
            if (props.TryGetValue(Locale, out var locale))
                device.LCID = Convert.ToInt32(locale, 16);
            if (props.TryGetValue(TimezoneName, out var tz))
                device.TZ = tz;
            if (props.TryGetValue(FriendlyName, out var friendlyName))
                device.FriendlyName = friendlyName;
            if (props.TryGetValue(ColorTheme, out var colorTheme))
                device.ColorTheme = int.Parse(colorTheme);
            if (props.TryGetValue(ColorAccent, out var colorAccent))
                device.ColorAccent = Convert.ToUInt32(colorAccent, 16);
            if (props.TryGetValue(PhoneNumber, out var phoneNumber))
                device.PhoneNumber = phoneNumber;
            if (props.TryGetValue(MobileOperator, out var mobileOperator))
                device.MobileOperator = mobileOperator;
            if (props.TryGetValue(CommercializedMobileOperator, out var cmo))
                device.CommercializedMobileOperator = cmo;
            if (props.TryGetValue(Imsi, out var imsi))
                device.IMSI = imsi;
            if (props.TryGetValue(SimIdHash, out var simId))
                device.SimId = simId;
            if (props.TryGetValue(MaxWorkingSet, out var maxWorkingSet))
                device.MaxWorkingSet = int.Parse(maxWorkingSet);
            if (props.TryGetValue(BatteryLevel, out var batteryLevel))
                device.BatteryLevel = int.Parse(batteryLevel);
            if (props.TryGetValue(PinLockEnabled, out var pinLocked))
                device.PinLocked = bool.Parse(pinLocked);
            if (props.TryGetValue(SimLockEnabled, out var simLocked))
                device.SimLocked = bool.Parse(simLocked);
            if (props.TryGetValue(StorageRemaining, out var storageRemaining))
                device.StorageRemaining = long.Parse(storageRemaining);
            if (props.TryGetValue(ScreenResolution, out var screenResolution))
                device.ScreenResolution = screenResolution;

            if (props.TryGetValue(LastLocation, out var lastLocationString))
                device.LastLocation = SkyDeviceLocation.Parse(lastLocationString);
        }
    }

}
