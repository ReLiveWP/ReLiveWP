using Grpc.Core;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Grpc.Mailbox;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Exchange.Services;

public class SettingsService(User.UserClient userClient, MailboxStore.MailboxStoreClient mailbox)
{
    public async Task<SettingsResponse> HandleAsync(
        string userId, string deviceId, SettingsRequest request, CancellationToken ct = default)
    {
        var response = new SettingsResponse();

        if (request.DeviceInformation?.Set is { } set)
            response.DeviceInformation = await HandleDeviceInformationAsync(userId, deviceId, set, ct);

        if (request.UserInformation?.Get is not null)
            response.UserInformation = await HandleUserInformation(userId);

        return response;
    }

    private async Task<SettingsDeviceInformationResponse> HandleDeviceInformationAsync(
        string userId, string deviceId, DeviceInformationSet set, CancellationToken ct)
    {
        var req = new UpsertDeviceInfoRequest { UserId = userId, DeviceId = deviceId };
        if (set.Model is not null)          req.Model           = set.Model;
        if (set.IMEI is not null)           req.Imei            = set.IMEI;
        if (set.FriendlyName is not null)   req.FriendlyName    = set.FriendlyName;
        if (set.OS is not null)             req.Os              = set.OS;
        if (set.OSLanguage is not null)     req.OsLanguage      = set.OSLanguage;
        if (set.PhoneNumber is not null)    req.PhoneNumber     = set.PhoneNumber;
        if (set.UserAgent is not null)      req.UserAgent       = set.UserAgent;
        if (set.EnableOutboundSMS.HasValue) req.EnableOutboundSms = set.EnableOutboundSMS.Value;
        if (set.MobileOperator is not null) req.MobileOperator  = set.MobileOperator;

        await mailbox.UpsertDeviceInfoAsync(req, cancellationToken: ct);

        return new SettingsDeviceInformationResponse { Set = new SettingsStatusOnly { Status = 1 } };
    }

    private async Task<SettingsUserInformationResponse> HandleUserInformation(string userId)
    {
        var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = userId });

        var email = userInfo.EmailAddress;
        var displayName = userInfo.Username;

        var account = new UserAccount
        {
            AccountName     = email ?? userId,
            UserDisplayName = displayName,
            SendDisabled    = 0,
            EmailAddresses  = email is not null ? new UserEmailAddresses
            {
                SMTPAddresses      = [email],
                PrimarySmtpAddress = email,
            } : null,
        };

        return new SettingsUserInformationResponse
        {
            Status = 1,
            Get    = new UserInformationResponseGet { Accounts = new UserAccounts { Items = [account] } },
        };
    }
}
