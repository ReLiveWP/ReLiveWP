namespace ReLiveWP.Backend.Identity.Data;

[Flags]
public enum LiveConnectedServiceFlags : uint
{
    None = 0,
    NeedsRefresh = 1,
    /// <summary>
    /// Indicates the service needs reauthentication
    /// </summary>
    Busted = 0x80000000
}
