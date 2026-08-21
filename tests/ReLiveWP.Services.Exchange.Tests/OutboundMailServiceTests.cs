using System.Text;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using ReLiveWP.Services.Exchange.Models;
using ReLiveWP.Services.Exchange.Services;
using ReLiveWP.Services.Grpc.Mail;
using ReLiveWP.Services.Grpc.Mailbox;
using MailClient = ReLiveWP.Services.Grpc.Mail.Mail.MailClient;

namespace ReLiveWP.Services.Exchange.Tests;

public class OutboundMailServiceTests
{
    private const string UserId = "user-wam";
    private const string Mime = "From: wam@relivewp.net\r\nTo: ada@relivewp.net\r\n\r\nhi\r\n";

    private readonly FakeMailClient mail = new();

    private OutboundMailService NewService() =>
        new(mail, new FakeMailboxClient(), NullLogger<OutboundMailService>.Instance);

    private Task<int> Submit(string? mime = Mime, string? clientId = null) =>
        NewService().SubmitAsync(UserId, mime, saveInSent: true, clientId, default);

    [Theory]
    [InlineData(SubmitStatus.Ok, EasStatus.Success)]
    [InlineData(SubmitStatus.NoRecipients, EasStatus.MessageHasNoRecipient)]
    [InlineData(SubmitStatus.UnresolvedRecipients, EasStatus.MessageRecipientUnresolved)]
    [InlineData(SubmitStatus.SenderNotAllowed, EasStatus.AccessDenied)]
    [InlineData(SubmitStatus.TooLarge, EasStatus.AttachmentIsTooLarge)]
    [InlineData(SubmitStatus.Malformed, EasStatus.MailSubmissionFailed)]
    [InlineData(SubmitStatus.Unspecified, EasStatus.MailSubmissionFailed)]
    public async Task Submit_statuses_map_onto_the_eas_codes(SubmitStatus submit, int expected)
    {
        mail.OnSubmit = _ => new SubmitResponse { Status = submit };

        Assert.Equal(expected, await Submit());
    }

    [Fact]
    public async Task An_empty_body_fails_without_calling_the_mail_service()
    {
        Assert.Equal(EasStatus.MailSubmissionFailed, await Submit(mime: null));
        Assert.Null(mail.LastRequest);
    }

    [Fact]
    public async Task A_dead_mail_service_reports_submission_failed_rather_than_throwing()
    {
        mail.OnSubmit = _ => throw new RpcException(new Status(StatusCode.Unavailable, "down"));

        Assert.Equal(EasStatus.MailSubmissionFailed, await Submit());
    }

    [Fact]
    public async Task Base64_wire_mime_is_decoded_before_submission()
    {
        mail.OnSubmit = _ => new SubmitResponse { Status = SubmitStatus.Ok };

        await Submit(Convert.ToBase64String(Encoding.Latin1.GetBytes(Mime)));

        Assert.Equal(Mime, Encoding.Latin1.GetString(mail.LastRequest!.Mime.ToByteArray()));
    }

    [Fact]
    public async Task Raw_wire_mime_is_passed_through_unchanged()
    {
        mail.OnSubmit = _ => new SubmitResponse { Status = SubmitStatus.Ok };

        await Submit();

        Assert.Equal(Mime, Encoding.Latin1.GetString(mail.LastRequest!.Mime.ToByteArray()));
    }

    [Fact]
    public async Task The_clients_id_becomes_the_submission_key_when_it_sends_one()
    {
        mail.OnSubmit = _ => new SubmitResponse { Status = SubmitStatus.Ok };

        await Submit(clientId: "wp7-42");

        Assert.Equal("client:wp7-42", mail.LastRequest!.ClientId);
    }

    // WP7 does not always send ClientId; without a stable key a resend would deliver twice
    [Fact]
    public async Task Without_a_client_id_the_same_bytes_produce_the_same_key()
    {
        mail.OnSubmit = _ => new SubmitResponse { Status = SubmitStatus.Ok };

        await Submit();
        var first = mail.LastRequest!.ClientId;
        await Submit();
        var second = mail.LastRequest!.ClientId;

        Assert.Equal(first, second);
        Assert.StartsWith("mime:", first);
    }

    [Fact]
    public async Task Different_messages_get_different_keys()
    {
        mail.OnSubmit = _ => new SubmitResponse { Status = SubmitStatus.Ok };

        await Submit();
        var first = mail.LastRequest!.ClientId;
        await Submit("From: wam@relivewp.net\r\nTo: ada@relivewp.net\r\n\r\ndifferent\r\n");

        Assert.NotEqual(first, mail.LastRequest!.ClientId);
    }

    // SubmitAsync never touches the mailbox; this only exists because the generated client's
    // parameterless constructor is protected
    private sealed class FakeMailboxClient : MailboxStore.MailboxStoreClient;

    private sealed class FakeMailClient : MailClient
    {
        public Func<SubmitRequest, SubmitResponse>? OnSubmit { get; set; }

        public SubmitRequest? LastRequest { get; private set; }

        public override AsyncUnaryCall<SubmitResponse> SubmitAsync(SubmitRequest request, CallOptions options)
        {
            LastRequest = request;

            Task<SubmitResponse> task;
            try
            {
                task = Task.FromResult(OnSubmit is null ? new SubmitResponse() : OnSubmit(request));
            }
            catch (Exception ex)
            {
                task = Task.FromException<SubmitResponse>(ex);
            }

            return new AsyncUnaryCall<SubmitResponse>(
                task,
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
        }
    }
}
