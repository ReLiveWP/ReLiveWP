using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Email;

namespace ReLiveWP.Backend.Mailbox.Services.Grpc;

public class EmailService(
    MailboxDbContext db,
    User.UserClient userClient) : Email.EmailBase
{
    public override async Task<Empty> SendSystemEmail(SendSystemEmailRequest request, ServerCallContext context)
    {
        var user = await userClient.GetUserInfoAsync(new GetUserInfoRequest() { UserId = request.UserId });

        var userInbox = await db.Folders.FirstOrDefaultAsync(f => 
            f.UserId == request.UserId && f.Type == DbFolderType.InboxDefault);
        if (userInbox == null)
            throw new RpcException(new Status(StatusCode.NotFound, "User's mailbox was not found."));

        var id = Guid.NewGuid().ToString("N");
        var email = new DbEmail()
        {
            Id = id,
            ServerId = id,
            UserId = userInbox.UserId,
            CollectionId = userInbox.Id,
            CreatedAt = DateTime.UtcNow,
            From = $"ReLiveWP ({request.ServiceName}) <noreply@relivewp.net>",
            To = user.EmailAddress,
            DisplayTo = user.EmailAddress,
            Subject = request.Subject,
            ThreadTopic = request.Subject,
            MessageClass = "IPM.Note",
            Importance = 1,
            Read = false,
            ContentClass = "urn:content-classes:message",
            DateReceived = DateTime.UtcNow,

            // TODO: the horrors of HTML email
            Body = $"{request.Body}\r\n\r\n----------------\r\nThis message was delivered by the ReLiveWP System, please do not reply.",
            BodyType = 1,
            NativeBodyType = 1,
        };

        db.Items.Add(email);
        await db.SaveChangesAsync();

        return new Empty();
    }
}