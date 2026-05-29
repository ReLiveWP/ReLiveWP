using System.Globalization;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Skybox.Data;
using ReLiveWP.Services.Grpc.FindMyPhone;

namespace ReLiveWP.Backend.Skybox.Services;

public class SkyboxDeviceService(SkyDbContext dbContext) : FindMyPhone.FindMyPhoneBase
{
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
            DeviceGuid = request.DeviceGuid,

            Make = request.DeviceProps["SkyProfile.Make"],
            Model = request.DeviceProps["SkyProfile.Model"],

            OSVersion = request.DeviceProps["SkyProfile.OSVersion"],
            ClientVersion = request.DeviceProps["SkyProfile.ClientVersion"],
            Capabilities = Convert.ToUInt32(request.DeviceProps["SkyProfile.Capabilities"], 16),
            LCID = Convert.ToInt32(request.DeviceProps["SkyProfile.Locale"], 16),
            TZ = request.DeviceProps["SkyProfile.TimezoneName"],
            FriendlyName = request.DeviceProps["SkyProfile.FriendlyName"],
            ColorTheme = int.Parse(request.DeviceProps["SkyProfile.ColorTheme"]),
            ColorAccent = Convert.ToUInt32(request.DeviceProps["SkyProfile.ColorAccent"], 16),
            PhoneNumber = request.DeviceProps.GetValueOrDefault("SkyProfile.PhoneNumber"),
            MobileOperator = request.DeviceProps.GetValueOrDefault("SkyProfile.MobileOperator"),
            CommercializedMobileOperator = request.DeviceProps.GetValueOrDefault("SkyProfile.CommercializedMobileOperator"),
            IMSI = request.DeviceProps.GetValueOrDefault("SkyProfile.Imsi"),
            SimId = request.DeviceProps.GetValueOrDefault("SkyProfile.SimIdHash"),

            MaxWorkingSet = int.Parse(request.DeviceProps["SkyProfile.MaxWorkingSet"]),
            BatteryLevel = int.Parse(request.DeviceProps["SkyProfile.BatteryLevel"]),
            PinLocked = bool.Parse(request.DeviceProps["SkyProfile.PinLockEnabled"]),
            SimLocked = bool.Parse(request.DeviceProps["SkyProfile.SimLockEnabled"]),
            StorageRemaining = long.Parse(request.DeviceProps["SkyProfile.StorageRemaining"]),
            ScreenResolution = request.DeviceProps.GetValueOrDefault("SkyProfile.ScreenResolution"),
        };

        dbContext.Devices.Add(device);
        await dbContext.SaveChangesAsync();


        return new RegisterDeviceResponse() { Code = 0, Enabled = true, Message = "S_OK" };
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
