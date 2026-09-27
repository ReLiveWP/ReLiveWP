namespace ReLiveWP.Backend.Identity.Services;

public class PermissiveInviteCodeValidator : IInviteCodeValidator
{
    public InviteCodeCheck CheckInviteCode(string inviteCode) => new(true, null);
}
