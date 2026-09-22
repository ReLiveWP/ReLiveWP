using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceRegistration.Data;
using ReLiveWP.Backend.DeviceRegistration.Model;
using ReLiveWP.Services.Grpc.DeviceRegistration;
using static ReLiveWP.Services.Grpc.DeviceRegistration.DeviceRegistration;

namespace ReLiveWP.Backend.DeviceRegistration.Services;

public class DeviceRegistrationService(ILogger<DeviceRegistrationService> logger,
                                       DevicesDbContext dbContext,
                                       IActivationCodeValidator activationCodeValidator) : DeviceRegistrationBase
{
    public override async Task<DeviceRegistrationResponse> RegisterDevice(DeviceRegistrationRequest request, ServerCallContext context)
    {
        logger.LogInformation("Registering device {DeviceID}...", request.UniqueId);

        var codeCheck = activationCodeValidator.CheckActivationCode(request.ActivationCode);
        if (!codeCheck.IsValid)
        {
            logger.LogWarning("Rejecting device {DeviceID}, activation code {ActivationCode} is not valid", request.UniqueId, request.ActivationCode);
            return RejectRegistration(ActivationRejection.InvalidActivationCode);
        }

        if (codeCheck.Serial is int serial && !await TryRedeemActivationCodeAsync(serial, request))
        {
            logger.LogWarning("Rejecting device {DeviceID}, activation code serial {Serial} is revoked or redeemed by another device", request.UniqueId, serial);
            return RejectRegistration(ActivationRejection.ActivationCodeInUse);
        }

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

    private async Task<bool> TryRedeemActivationCodeAsync(int serial, DeviceRegistrationRequest request)
    {
        var existingRedemption = await dbContext.ActivationCodeRedemptions.FindAsync(serial);
        if (existingRedemption != null)
            return existingRedemption.RevokedAt == null && existingRedemption.DeviceUniqueId == request.UniqueId;

        var redemption = new ActivationCodeRedemption()
        {
            Serial = serial,
            DeviceUniqueId = request.UniqueId,
            ActivationCode = request.ActivationCode,
            RedeemedAt = DateTimeOffset.UtcNow
        };

        dbContext.ActivationCodeRedemptions.Add(redemption);
        try
        {
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Lost the race redeeming activation code serial {Serial}", serial);
            dbContext.Entry(redemption).State = EntityState.Detached;
            return false;
        }
    }

    private static DeviceRegistrationResponse RejectRegistration(ActivationRejection rejection) =>
        new() { Succeeded = false, Rejection = rejection };

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

    public override async Task DevicesForUser(DevicesForUserRequest request, IServerStreamWriter<Device> responseStream, ServerCallContext context)
    {
        foreach (var device in dbContext.Devices.Where(d => d.OwnerId == request.UserId))
        {
            var resp = new Device()
            {
                Manufacturer = device.Manufacturer,
                Model = device.Model,
                Operator = device.Operator,
                Imei = device.IMEI,
                OsVersion = device.OSVersion,
                Locale = device.Locale,
                UniqueId = device.UniqueId,
            };

            await responseStream.WriteAsync(resp);
        }
    }

    public override async Task<Device> DeviceById(DeviceByIdRequest request, ServerCallContext context)
    {
        var device = await dbContext.Devices.FirstOrDefaultAsync(r => r.UniqueId == request.DeviceId.ToUpperInvariant())
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Device not found!"));

        var resp = new Device()
        {
            Manufacturer = device.Manufacturer,
            Model = device.Model,
            Operator = device.Operator,
            Imei = device.IMEI,
            OsVersion = device.OSVersion,
            Locale = device.Locale,
            UniqueId = device.UniqueId,
        };

        return resp;
    }
}
