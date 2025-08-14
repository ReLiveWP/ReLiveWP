using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceRegistration.Database;
using ReLiveWP.Backend.DeviceRegistration.Model;

namespace ReLiveWP.Backend.DeviceRegistration;

public class DeviceRegistrationService(ILogger<DeviceRegistrationService> logger,
                                       DevicesDbContext dbContext)
    : DeviceRegistration.DeviceRegistrationBase
{
    public override async Task<DeviceRegistrationResponse> RegisterDevice(DeviceRegistrationRequest request, ServerCallContext context)
    {
        logger.LogInformation("Registering device {DeviceID}...", request.UniqueId);

        var deviceRegistrationResponse = new DeviceRegistrationResponse()
        {
            Succeeded = true,
            WasAlreadyRegistered = true
        };

        var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.UniqueId == request.UniqueId);
        if (device == null)
        {
            deviceRegistrationResponse.WasAlreadyRegistered = false;
            device = new DeviceModel()
            {
                Id = Guid.NewGuid().ToString(),

                UniqueId = request.UniqueId,
                CertificateSubject = request.CertificateSubject,
                OSVersion = request.OsVersion,
                Locale = request.Locale,

                Manufacturer = request.DeviceManufacturer,
                Model = request.DeviceModel,
                Operator = request.DeviceOperator,
                IMEI = request.DeviceIMEI
            };

            await dbContext.Devices.AddAsync(device);
        }
        else
        {
            device.CertificateSubject = request.CertificateSubject;
            device.OSVersion = request.OsVersion;
            device.Locale = request.Locale;
            device.Model = request.DeviceModel;
            device.Operator = request.DeviceOperator;
            device.IMEI = request.DeviceIMEI; // shouldn't change 
        }

        await dbContext.SaveChangesAsync();
        return deviceRegistrationResponse;
    }
}
