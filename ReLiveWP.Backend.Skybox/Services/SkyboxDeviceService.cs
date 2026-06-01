using System.Globalization;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Skybox.Data;
using ReLiveWP.Services.Grpc.FindMyPhone;

namespace ReLiveWP.Backend.Skybox.Services;

public class SkyboxDeviceService(SkyDbContext dbContext) : FindMyPhone.FindMyPhoneBase
{
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
        }
    }

    public override async Task<RegisterDeviceResponse> RegisterDevice(RegisterDeviceRequest request, ServerCallContext context)
    {
        var userId = new Guid(request.UserId);
        var existing = await dbContext.Devices.FirstOrDefaultAsync(d => d.DeviceGuid == request.DeviceGuid);
        if (existing != null)
        {
            dbContext.Devices.Remove(existing);
        }

        var device = new SkyDevice()
        {
            OwnerId = userId,

            // Correlates to UniqueId in DeviceRegistration
            DeviceGuid = request.DeviceGuid
        };

        SkyProfileKeys.ApplyProperties(device, request.DeviceProps);

        dbContext.Devices.Add(device);
        await dbContext.SaveChangesAsync();


        return new RegisterDeviceResponse() { Code = 0, Enabled = true, Message = "OK" };
    }

    public override async Task<RegisterChannelResponse> RegisterChannel(RegisterChannelRequest request, ServerCallContext context)
    {
        var device = await dbContext.Devices.AsTracking().FirstOrDefaultAsync(d => d.DeviceGuid == request.DeviceGuid)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Device not found!"));

        if (!Uri.TryCreate(device.NotificationChannelUrl, UriKind.Absolute, out var uri) || uri.Host != "push.relivewp.net" || uri.Scheme != Uri.UriSchemeHttps)
        {
            device.NotificationChannelUrl = null;
        }
        else
        {
            device.NotificationChannelUrl = request.NotificationUri;
        }

        await dbContext.SaveChangesAsync();
        return new RegisterChannelResponse() { Code = 0, Enabled = true, Message = "OK" };
    }

    public override async Task<UpdateDeviceInfoResponse> UpdateDeviceInfo(UpdateDeviceInfoRequest request, ServerCallContext context)
    {
        var device = await dbContext.Devices.AsTracking().FirstOrDefaultAsync(d => d.DeviceGuid == request.DeviceGuid)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Device not found!"));

        SkyProfileKeys.ApplyProperties(device, request.DeviceProps);

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
                BatteryLevel = device.BatteryLevel,
                StorageRemaining = device.StorageRemaining,
                PinLocked = device.PinLocked,
                SimLocked = device.SimLocked
            };

            if (!string.IsNullOrEmpty(device.MobileOperator))
                resp.Operator = device.MobileOperator;

            if (!string.IsNullOrWhiteSpace(device.PhoneNumber))
                resp.PhoneNumber = device.PhoneNumber;

            await responseStream.WriteAsync(resp);
        }
    }


}
