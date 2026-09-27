namespace ReLiveWP.Services.Login.Utilities;

public static class PassportErrors
{
    public const uint BadMemberNameOrPassword = 0x80048821;
    public const uint Unauthenticated = 0x80048800;

    public static string Describe(uint code) => code switch
    {
        BadMemberNameOrPassword => "The sign-in name or password does not match one in the ReLive account system.",
        _ => $"Something went wrong signing you in. (0x{code:X8})"
    };
}
