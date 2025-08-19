using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceRegistration.Data;
using ReLiveWP.Backend.DeviceRegistration.Model;
using ReLiveWP.Services.Grpc;
using static ReLiveWP.Services.Grpc.DeviceRegistration;

namespace ReLiveWP.Backend.DeviceRegistration.Services;

public class DeviceRegistrationService(ILogger<DeviceRegistrationService> logger,
                                       DevicesDbContext dbContext) : DeviceRegistrationBase
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

    public override async Task<DeviceAssociationResponse> AssociateDeviceWithUser(DeviceAssociationRequest request, ServerCallContext context)
    {
        var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.UniqueId == request.DeviceId);
        if (device == null)
        {
            return new DeviceAssociationResponse() { Succeeded = false };
        }

        device.OwnerId = request.UserId;
        await dbContext.SaveChangesAsync();

        return new DeviceAssociationResponse() { Succeeded = true };
    }
}
