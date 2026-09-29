using System.ServiceModel;
using ReLiveWP.Identity.Soap;
using ReLiveWP.ServiceDefaults.Contacts;
using ReLiveWP.Services.AddressBook.Models;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.AddressBook.Services;

[ServiceContract(Namespace = AddressBookConstants.Ns)]
public interface IAddressBookService
{
    [OperationContract(Action = nameof(ViewABNetworks))]
    Task<ViewABNetworksResponse> ViewABNetworks(ViewABNetworks message);
}

public class AddressBookService(
    SoapTicketVerifier tickets,
    User.UserClient userClient,
    ILogger<AddressBookService> logger) : IAddressBookService
{
    public static readonly string[] TicketTargets =
    [
        "http://Passport.NET/tb",
        "relivewp.net",
        "contacts.relivewp.net",
        "contacts.int.relivewp.net",
    ];

    // ContactAgg | DashboardAgg | StatusPublish | CommentPublish
    private const int RequiredOffers = 0x851;

    public async Task<ViewABNetworksResponse> ViewABNetworks(ViewABNetworks message)
    {
        var ticket = await tickets.VerifyAsync(message.ABAuthHeader?.TicketToken, TicketTargets);
        if (!ticket.IsValid)
            throw new FaultException(ticket.FailureMessage);

        var displayName = await GetDisplayNameAsync(ticket);

        var now = DateTime.UtcNow;
        var response = new ViewABNetworksResponse();
        response.ViewABNetworksResult.Add(new NetworkInfo()
        {
            DomainId = AggregateNetwork.DomainId,
            SourceId = AggregateNetwork.DomainTag,
            DomainTag = AggregateNetwork.DomainTag,
            DisplayName = displayName,
            RelationshipType = 3,
            RelationshipState = 3,
            RelationshipStateDate = now,
            CreateDate = now,
            LastChanged = now,
            Annotations =
            [
                new() { Name = "Live.Network.PSAState", Value = "Accept" },
                // TODO: this parses with LOCALE_SYSTEM_DEFAULT on device, which feels like the footgun of all time
                new() { Name = "Live.Network.LastSync", Value = DateTime.Now.ToString() },
                new() { Name = "Live.Network.Offers", Value = RequiredOffers.ToString() }
            ]
        });

        return response;
    }

    // an empty DisplayName is a delink on the device, so this never comes back empty
    private async Task<string> GetDisplayNameAsync(SoapTicketResult ticket)
    {
        try
        {
            var userInfo = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = ticket.UserId });
            if (!string.IsNullOrWhiteSpace(userInfo.Username))
                return userInfo.Username;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read the user info for {UserId}", ticket.UserId);
        }

        return ticket.Claim("preferred_username") ?? "User";
    }
}
