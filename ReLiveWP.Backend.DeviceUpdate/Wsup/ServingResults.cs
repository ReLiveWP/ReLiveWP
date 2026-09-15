using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public record SyncUpdatesResult(
    string Xml,
    DeviceRing Ring,
    int ReportedInstalled,
    int ReportedCached,
    int NewCount,
    int ChangedCount,
    int InstallCount,
    int BundleCount,
    int BlockedCount,
    int OutOfScopeCount,
    bool Truncated)
{
    public int OfferedCount => NewCount + ChangedCount;
}

public record GetExtendedUpdateInfoResult(
    string Xml,
    int RequestedCount,
    int FragmentCount,
    int FileLocationCount,
    int OutOfScopeCount);
