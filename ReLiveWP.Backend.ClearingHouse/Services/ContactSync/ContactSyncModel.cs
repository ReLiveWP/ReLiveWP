using ReLiveWP.Backend.ClearingHouse.Services.Mirror;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public sealed record PhotoCrop(int X, int Y, int Width, int Height, bool OriginIsBottomLeft);

public sealed record RemoteContact(
    string ExternalId,
    ContactItem Contact,
    string? Etag = null,
    string? PhotoUrl = null,
    byte[]? PhotoData = null,
    string? PhotoServiceId = null,
    PhotoCrop? PhotoCrop = null) : IRemoteItem
{
    public void ApplyTo(CreateItemRequest request) => request.Contact = Contact;

    public void ApplyTo(UpdateItemRequest request) => request.Contact = Contact;
}
