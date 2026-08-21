using System.Text;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.Mail.Grpc;
using ReLiveWP.Backend.Mail.Services;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mail;

namespace ReLiveWP.Backend.Mail.Tests;

public class MailSubmissionServiceTests
{
    private const string UserId = "user-wam";
    private const string Sender = "wam@relivewp.net";

    private readonly FakeMailQueue queue = new();
    private readonly FakeSentItemsWriter sentItems = new();
    private readonly FakeUserClient users = new();

    public MailSubmissionServiceTests()
    {
        users.OnGetUserInfo = _ => new GetUserInfoResponse { EmailAddress = Sender };
        users.OnLookupUsersByEmail = _ =>
        {
            var response = new LookupUsersByEmailResponse();
            response.Users.Add(new DirectoryUser { QueriedEmail = "ada@relivewp.net", UserId = "user-ada" });
            return response;
        };
    }

    private MailSubmissionService NewService(params MailRoute[] routes) =>
        new(new RecipientRouter(users, Options.Create(new MailOptions { LocalDomains = ["relivewp.net"] })),
            queue,
            sentItems,
            routes.Select(r => (IMailDeliveryAgent)new FakeDeliveryAgent(r)),
            users,
            Options.Create(new MailOptions { LocalDomains = ["relivewp.net"] }),
            NullLogger<MailSubmissionService>.Instance);

    private static SubmitRequest Request(string mime, bool saveInSent = false) => new()
    {
        UserId = UserId,
        Mime = ByteString.CopyFrom(Encoding.Latin1.GetBytes(mime)),
        SaveInSentItems = saveInSent,
    };

    private const string ToAda =
        "From: wam@relivewp.net\r\nTo: ada@relivewp.net\r\nSubject: hi\r\n\r\nhello\r\n";

    [Fact]
    public async Task A_local_message_is_accepted_and_queued()
    {
        var response = await NewService(MailRoute.Local).Submit(Request(ToAda), new StubCallContext());

        Assert.Equal(SubmitStatus.Ok, response.Status);
        var queued = Assert.Single(queue.Enqueued);
        Assert.Equal("user-ada", Assert.Single(queued.Envelope.Recipients).UserId);
        Assert.Equal(response.SubmissionId, queued.Envelope.SubmissionId);
    }

    [Fact]
    public async Task Garbage_input_is_rejected_as_malformed()
    {
        var request = new SubmitRequest { UserId = UserId, Mime = ByteString.CopyFrom([0x00, 0xff]) };

        var response = await NewService(MailRoute.Local).Submit(request, new StubCallContext());

        Assert.Equal(SubmitStatus.Malformed, response.Status);
        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task A_message_with_no_recipients_is_rejected()
    {
        var response = await NewService(MailRoute.Local).Submit(
            Request("From: wam@relivewp.net\r\nSubject: hi\r\n\r\nhello\r\n"), new StubCallContext());

        Assert.Equal(SubmitStatus.NoRecipients, response.Status);
        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task Sending_as_somebody_else_is_refused()
    {
        var response = await NewService(MailRoute.Local).Submit(
            Request("From: ada@relivewp.net\r\nTo: ada@relivewp.net\r\n\r\nhi\r\n"), new StubCallContext());

        Assert.Equal(SubmitStatus.SenderNotAllowed, response.Status);
        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task An_unknown_local_address_comes_back_unresolved()
    {
        var response = await NewService(MailRoute.Local).Submit(
            Request("From: wam@relivewp.net\r\nTo: nobody@relivewp.net\r\n\r\nhi\r\n"), new StubCallContext());

        Assert.Equal(SubmitStatus.UnresolvedRecipients, response.Status);
        Assert.Equal(RecipientRoute.Unroutable, Assert.Single(response.Recipients).Route);
        Assert.Empty(queue.Enqueued);
    }

    // phase 1 registers no external agent, so an outside address is reported rather than queued.
    // phase 3 registers one and this same path starts succeeding.
    [Fact]
    public async Task An_external_address_is_unresolved_while_no_agent_handles_it()
    {
        var response = await NewService(MailRoute.Local).Submit(
            Request("From: wam@relivewp.net\r\nTo: someone@gmail.com\r\n\r\nhi\r\n"), new StubCallContext());

        Assert.Equal(SubmitStatus.UnresolvedRecipients, response.Status);
        Assert.Equal(RecipientRoute.External, Assert.Single(response.Recipients).Route);
        Assert.Empty(queue.Enqueued);
    }

    [Fact]
    public async Task An_external_address_is_queued_once_an_agent_handles_it()
    {
        var response = await NewService(MailRoute.Local, MailRoute.External).Submit(
            Request("From: wam@relivewp.net\r\nTo: someone@gmail.com\r\n\r\nhi\r\n"), new StubCallContext());

        Assert.Equal(SubmitStatus.Ok, response.Status);
        Assert.Single(queue.Enqueued);
    }

    [Fact]
    public async Task Bcc_recipients_are_delivered_but_stripped_from_the_delivered_bytes()
    {
        users.OnLookupUsersByEmail = _ =>
        {
            var response = new LookupUsersByEmailResponse();
            response.Users.Add(new DirectoryUser { QueriedEmail = "ada@relivewp.net", UserId = "user-ada" });
            response.Users.Add(new DirectoryUser { QueriedEmail = "grace@relivewp.net", UserId = "user-grace" });
            return response;
        };

        var response = await NewService(MailRoute.Local).Submit(
            Request("From: wam@relivewp.net\r\nTo: ada@relivewp.net\r\n" +
                    "Bcc: grace@relivewp.net\r\nSubject: hi\r\n\r\nhello\r\n",
                saveInSent: true),
            new StubCallContext());

        Assert.Equal(SubmitStatus.Ok, response.Status);

        var queued = Assert.Single(queue.Enqueued);
        Assert.Equal(2, queued.Envelope.Recipients.Count);

        var delivered = Encoding.Latin1.GetString(queued.Message);
        Assert.DoesNotContain("grace@relivewp.net", delivered);

        // the sender's own copy keeps the blind list
        var sent = Encoding.Latin1.GetString(Assert.Single(sentItems.Written).Message);
        Assert.Contains("grace@relivewp.net", sent);
    }

    [Fact]
    public async Task A_failed_sender_copy_does_not_sink_the_submission()
    {
        sentItems.OnWrite = () => throw new InvalidOperationException("mailbox down");

        var response = await NewService(MailRoute.Local).Submit(
            Request(ToAda, saveInSent: true), new StubCallContext());

        Assert.Equal(SubmitStatus.Ok, response.Status);
        Assert.Single(queue.Enqueued);
    }

    [Fact]
    public async Task A_client_id_becomes_the_submission_id_so_a_retry_lands_once()
    {
        var request = Request(ToAda);
        request.ClientId = "sendmail-42";

        var response = await NewService(MailRoute.Local).Submit(request, new StubCallContext());

        Assert.Equal("sendmail-42", response.SubmissionId);
        Assert.Equal("sendmail-42", Assert.Single(queue.Enqueued).Envelope.SubmissionId);
    }

    [Fact]
    public async Task An_oversized_message_is_rejected()
    {
        var service = new MailSubmissionService(
            new RecipientRouter(users, Options.Create(new MailOptions { LocalDomains = ["relivewp.net"] })),
            queue,
            sentItems,
            [new FakeDeliveryAgent(MailRoute.Local)],
            users,
            Options.Create(new MailOptions { LocalDomains = ["relivewp.net"], MaxMessageBytes = 16 }),
            NullLogger<MailSubmissionService>.Instance);

        var response = await service.Submit(Request(ToAda), new StubCallContext());

        Assert.Equal(SubmitStatus.TooLarge, response.Status);
        Assert.Empty(queue.Enqueued);
    }
}
