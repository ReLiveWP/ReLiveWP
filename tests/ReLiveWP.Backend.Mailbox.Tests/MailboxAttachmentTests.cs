using Microsoft.Extensions.Logging.Abstractions;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.Mailbox.Data;
using ReLiveWP.Backend.Mailbox.Data.Entities;
using ReLiveWP.Backend.Mailbox.Services;
using ReLiveWP.Backend.Mailbox.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Backend.Mailbox.Tests;

// covers the 25MB attachment write cap (block C) and the read-path attachment Content projection
// (block A) -- specifically that excluding Content from LoadChildrenAsync's query never causes
// UpdateItem to try to re-insert an email's existing attachments.
public class MailboxAttachmentTests : IDisposable
{
    private const string FolderId = "inbox-folder";
    private const string UserId = "user-1";

    private readonly SqliteConnection _connection;

    public MailboxAttachmentTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
        db.Folders.Add(new DbFolder { Id = FolderId, UserId = UserId, DisplayName = "Inbox", Type = DbFolderType.InboxDefault });
        db.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private MailboxDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<MailboxDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new MailboxDbContext(options);
    }

    private MailboxStoreService NewService(MailboxDbContext db) =>
        new(db, new MailboxIntegrityService(db), new SyncStateRepairService(db), new MailboxDeletionService(db), FakeUserClient.NoLinks(db));

    private static ServerCallContext NewCallContext() => new StubCallContext();

    private static CreateItemRequest NewEmailRequest(params AttachmentItem[] attachments)
    {
        var req = new CreateItemRequest
        {
            UserId = UserId,
            CollectionId = FolderId,
            Email = new EmailItem { Subject = "hi" },
        };
        req.Email.Attachments.AddRange(attachments);
        return req;
    }

    [Fact]
    public async Task CreateItem_rejects_an_oversized_attachment()
    {
        var oversized = new AttachmentItem
        {
            Id = "att-1",
            Content = ByteString.CopyFrom(new byte[25 * 1024 * 1024 + 1]),
        };

        await using var db = NewContext();
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => NewService(db).CreateItem(NewEmailRequest(oversized), NewCallContext()));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task CreateItem_accepts_an_attachment_at_the_cap()
    {
        var atCap = new AttachmentItem
        {
            Id = "att-1",
            Content = ByteString.CopyFrom(new byte[25 * 1024 * 1024]),
        };

        await using var db = NewContext();
        var item = await NewService(db).CreateItem(NewEmailRequest(atCap), NewCallContext());

        Assert.Single(item.Email.Attachments);
    }

    [Fact]
    public async Task GetItem_never_loads_attachment_Content_but_GetAttachmentContent_still_does()
    {
        string emailId;
        await using (var db = NewContext())
        {
            var item = await NewService(db).CreateItem(NewEmailRequest(new AttachmentItem
            {
                Id = "att-1",
                DisplayName = "a.txt",
                Content = ByteString.CopyFrom([1, 2, 3]),
            }), NewCallContext());
            emailId = item.ServerId;
        }

        await using var db2 = NewContext();
        var fetched = await NewService(db2).GetItem(new GetItemRequest { UserId = UserId, ServerId = emailId }, NewCallContext());
        var attachment = Assert.Single(fetched.Email.Attachments);
        Assert.Equal("a.txt", attachment.DisplayName);
        Assert.False(attachment.HasContent); // never populated on this path

        var content = await NewService(db2).GetAttachmentContent(
            new GetAttachmentContentRequest { UserId = UserId, AttachmentId = attachment.Id }, NewCallContext());
        Assert.Equal(new byte[] { 1, 2, 3 }, content.Content.ToByteArray());
    }

    [Fact]
    public async Task UpdateItem_on_an_email_with_existing_attachments_does_not_duplicate_them()
    {
        string emailId;
        await using (var db = NewContext())
        {
            var item = await NewService(db).CreateItem(NewEmailRequest(new AttachmentItem
            {
                Id = "att-1",
                DisplayName = "a.txt",
                Content = ByteString.CopyFrom([1, 2, 3]),
            }), NewCallContext());
            emailId = item.ServerId;
        }

        // UpdateItem never touches Attachments; this must not throw (duplicate-key insert) and
        // must not change the attachment count, even though the projected read-load has no Content
        await using (var db = NewContext())
        {
            var result = await NewService(db).UpdateItem(new UpdateItemRequest
            {
                UserId = UserId,
                ServerId = emailId,
                Email = new EmailItem { Subject = "updated subject" },
            }, NewCallContext());
            Assert.True(result.Found);
        }

        await using var verify = NewContext();
        var attachments = await verify.Attachments.Where(a => a.EmailItemId == emailId).ToListAsync();
        var attachment = Assert.Single(attachments);
        Assert.Equal(new byte[] { 1, 2, 3 }, attachment.Content);

        var updated = await NewService(verify).GetItem(new GetItemRequest { UserId = UserId, ServerId = emailId }, NewCallContext());
        Assert.Equal("updated subject", updated.Email.Subject);
    }
}
