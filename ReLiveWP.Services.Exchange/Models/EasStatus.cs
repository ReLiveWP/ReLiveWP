namespace ReLiveWP.Services.Exchange.Models;

// MS-ASCMD 2.2.2 common status codes, as used by the ComposeMail commands
public static class EasStatus
{
    public const int Success = 0;
    public const int MessageRecipientUnresolved = 116;
    public const int MessageHasNoRecipient = 119;
    public const int MailSubmissionFailed = 120;
    public const int AttachmentIsTooLarge = 122;
    public const int AccessDenied = 130;
}
