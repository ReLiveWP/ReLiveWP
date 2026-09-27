namespace ReLiveWP.Backend.Identity.Services;

public readonly record struct InviteCodeCheck(bool IsValid, int? Serial);

public interface IInviteCodeValidator
{
    InviteCodeCheck CheckInviteCode(string inviteCode);
}
