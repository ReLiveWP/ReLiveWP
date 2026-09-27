namespace ReLiveWP.Backend.ConnectedServices.Data;

[Flags]
public enum LiveConnectedServiceFlags : uint
{
    None = 0,
    NeedsRefresh = 1,
    Transient = 2,

    Busted = 0x80000000
}
