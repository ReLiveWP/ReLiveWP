namespace ReLiveWP.Services.Messenger.Msnp;

public enum MsnpErrorCode
{
    InvalidRecipient = 201,
    RecipientNotOnline = 217,
    InternalServerError = 500,
    Throttled = 800,
    AuthenticationFailed = 911,
}
