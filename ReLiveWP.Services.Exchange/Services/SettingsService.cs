using Microsoft.EntityFrameworkCore;
using ReLiveWP.Services.Exchange.Data;
using ReLiveWP.Services.Exchange.Data.Entities;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Services;

// Handles the Settings command: stores DeviceInformation and returns UserInformation.
public class SettingsService
{
    private readonly ExchangeDbContext _db;

    public SettingsService(ExchangeDbContext db) => _db = db;

    public async Task<SettingsResponse> HandleAsync(
        string userId, string deviceId, SettingsRequest request, CancellationToken ct = default)
    {
        var response = new SettingsResponse();

        if (request.DeviceInformation?.Set is { } set)
            response.DeviceInformation = await HandleDeviceInformationAsync(userId, deviceId, set, ct);

        if (request.UserInformation?.Get is not null)
            response.UserInformation = HandleUserInformation(userId);

        return response;
    }

    private async Task<SettingsDeviceInformationResponse> HandleDeviceInformationAsync(
        string userId, string deviceId, DeviceInformationSet set, CancellationToken ct)
    {
        var info = await _db.DeviceInfos.SingleOrDefaultAsync(
            d => d.UserId == userId && d.DeviceId == deviceId, ct);

        if (info is null)
        {
            info = new DeviceInfo { UserId = userId, DeviceId = deviceId };
            _db.DeviceInfos.Add(info);
        }

        info.Model = set.Model;
        info.IMEI = set.IMEI;
        info.FriendlyName = set.FriendlyName;
        info.OS = set.OS;
        info.OSLanguage = set.OSLanguage;
        info.PhoneNumber = set.PhoneNumber;
        info.UserAgent = set.UserAgent;
        info.EnableOutboundSMS = set.EnableOutboundSMS;
        info.MobileOperator = set.MobileOperator;
        info.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new SettingsDeviceInformationResponse
        {
            Set = new SettingsStatusOnly { Status = 1 },
        };
    }

    private SettingsUserInformationResponse HandleUserInformation(string userId)
    {
        // Protocol 14.1: EmailAddresses belongs inside Accounts/Account, not under Get directly.
        // PrimarySmtpAddress identifies the default sending address for SendMail.
        var email = userId.Contains('@') ? userId : null;
        var displayName = email is not null
            ? email[..email.IndexOf('@')]
            : userId;

        var account = new UserAccount
        {
            // Primary account has no AccountId per spec
            AccountName = email ?? userId,
            UserDisplayName = displayName,
            SendDisabled = 0,
            EmailAddresses = email is not null ? new UserEmailAddresses
            {
                SMTPAddresses = [email],
                PrimarySmtpAddress = email,
            } : null,
        };

        return new SettingsUserInformationResponse
        {
            Status = 1,
            Get = new UserInformationResponseGet
            {
                Accounts = new UserAccounts { Items = [account] },
            },
        };
    }
}
