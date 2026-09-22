using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Backend.DeviceRegistration.Data;
using ReLiveWP.Backend.DeviceRegistration.Model;
using ReLiveWP.Backend.DeviceRegistration.Services;
using ReLiveWP.Services.Grpc.DeviceRegistration;

namespace ReLiveWP.Backend.DeviceRegistration.Tests;

public class ActivationCodeRedemptionTests : IDisposable
{
    private const string FirstDevice = "791225FE814E188DCD3246951E40986F2013D7DF";
    private const string SecondDevice = "050021D9A03C58BE819B92D5D44425CA2A26727B";

    private readonly SqliteConnection _connection;
    private readonly DevicesDbContext _db;

    public ActivationCodeRedemptionTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _db = new DevicesDbContext(
            new DbContextOptionsBuilder<DevicesDbContext>().UseSqlite(_connection).Options);

        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class FixedValidator(ActivationCodeCheck check) : IActivationCodeValidator
    {
        public ActivationCodeCheck CheckActivationCode(string activationCode) => check;
    }

    private DeviceRegistrationService CreateService(IActivationCodeValidator validator) =>
        new(NullLogger<DeviceRegistrationService>.Instance, _db, validator);

    private DeviceRegistrationService CreateServiceForSerial(int serial) =>
        CreateService(new FixedValidator(new ActivationCodeCheck(true, serial)));

    private static DeviceRegistrationRequest CreateRequest(string uniqueId, string activationCode = "JDJ77-4XR9F-WFRRP-MGRYW-HB9MB") => new()
    {
        UniqueId = uniqueId,
        ActivationCode = activationCode,
        CertificateSubject = "CN=urn:wp-ac-hash:test",
        OsVersion = "7.10.8773",
        Locale = "0409",
    };

    [Fact]
    public async Task InvalidCodeIsRejectedWithoutRegistering()
    {
        var service = CreateService(new FixedValidator(new ActivationCodeCheck(false, null)));

        var response = await service.RegisterDevice(CreateRequest(FirstDevice), null!);

        Assert.False(response.Succeeded);
        Assert.Equal(ActivationRejection.InvalidActivationCode, response.Rejection);
        Assert.Empty(_db.Devices);
        Assert.Empty(_db.ActivationCodeRedemptions);
    }

    [Fact]
    public async Task PermissiveValidatorRegistersWithoutRedeeming()
    {
        var service = CreateService(new PermissiveActivationCodeValidator());

        var response = await service.RegisterDevice(CreateRequest(FirstDevice, "AAAAA-AAAAA-AAAAA-AAAAA-AAAAA"), null!);

        Assert.True(response.Succeeded);
        Assert.Equal(ActivationRejection.None, response.Rejection);
        Assert.Single(_db.Devices);
        Assert.Empty(_db.ActivationCodeRedemptions);
    }

    [Fact]
    public async Task FirstRedemptionBindsSerialToDevice()
    {
        var service = CreateServiceForSerial(1_000_000);

        var response = await service.RegisterDevice(CreateRequest(FirstDevice), null!);

        Assert.True(response.Succeeded);
        var redemption = Assert.Single(_db.ActivationCodeRedemptions);
        Assert.Equal(1_000_000, redemption.Serial);
        Assert.Equal(FirstDevice, redemption.DeviceUniqueId);
        Assert.Null(redemption.RevokedAt);
    }

    [Fact]
    public async Task SameDeviceCanRedeemAgain()
    {
        var service = CreateServiceForSerial(1_000_000);

        await service.RegisterDevice(CreateRequest(FirstDevice), null!);
        var response = await service.RegisterDevice(CreateRequest(FirstDevice), null!);

        Assert.True(response.Succeeded);
        Assert.True(response.WasAlreadyRegistered);
        Assert.Single(_db.ActivationCodeRedemptions);
    }

    [Fact]
    public async Task OtherDeviceIsRejected()
    {
        var service = CreateServiceForSerial(1_000_000);

        await service.RegisterDevice(CreateRequest(FirstDevice), null!);
        var response = await service.RegisterDevice(CreateRequest(SecondDevice), null!);

        Assert.False(response.Succeeded);
        Assert.Equal(ActivationRejection.ActivationCodeInUse, response.Rejection);
        Assert.DoesNotContain(_db.Devices, device => device.UniqueId == SecondDevice);
    }

    [Fact]
    public async Task RevokedSerialIsRejectedEvenForItsOwnDevice()
    {
        _db.ActivationCodeRedemptions.Add(new ActivationCodeRedemption()
        {
            Serial = 1_000_000,
            DeviceUniqueId = FirstDevice,
            ActivationCode = "JDJ77-4XR9F-WFRRP-MGRYW-HB9MB",
            RedeemedAt = DateTimeOffset.UtcNow,
            RevokedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync();

        var service = CreateServiceForSerial(1_000_000);
        var response = await service.RegisterDevice(CreateRequest(FirstDevice), null!);

        Assert.False(response.Succeeded);
        Assert.Equal(ActivationRejection.ActivationCodeInUse, response.Rejection);
    }

    [Fact]
    public async Task DifferentSerialsOnOneDeviceBothRedeem()
    {
        await CreateServiceForSerial(1_000_000).RegisterDevice(CreateRequest(FirstDevice), null!);
        var response = await CreateServiceForSerial(1_000_001).RegisterDevice(CreateRequest(FirstDevice), null!);

        Assert.True(response.Succeeded);
        Assert.Equal(2, _db.ActivationCodeRedemptions.Count());
    }
}
