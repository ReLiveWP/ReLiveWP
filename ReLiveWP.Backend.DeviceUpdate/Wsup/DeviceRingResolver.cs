using ReLiveWP.Backend.DeviceUpdate.Model;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

// Works out whether the device on the other end is carrying our shim, from the two id lists a
// SyncUpdates request already carries. Pure, so the interesting cases are testable without a
// catalog; UpdateService supplies the marker's revision ids.
public static class DeviceRingResolver
{
    // The marker detectoid authored in Catalog/relivewp.xml. It queries the ROMPackage CSP for
    // Pkg_ReLiveWP, so the device answers it without anything having to be written first.
    public static readonly Guid MarkerUpdateId = new("6f2a1c74-9d3e-4b58-8f10-2c7e5a6d0b41");

    // Three states, not two. A detectoid the client evaluated as false is cached rather than
    // reported, so "reported" and "cached but not reported" are genuinely different from "never
    // seen it", and only the third one is ambiguous.
    public static DeviceRing Resolve(
        IReadOnlyCollection<long> markerRevisions,
        IReadOnlySet<long> installedNonLeaf,
        IReadOnlySet<long> otherCached)
    {
        if (markerRevisions.Any(installedNonLeaf.Contains))
            return DeviceRing.ReLiveWP;

        if (markerRevisions.Any(otherCached.Contains))
            return DeviceRing.Stock;

        return DeviceRing.Unknown;
    }

    // Which ring's deployment rows a device is served. Unknown deliberately resolves to ReLiveWP:
    // a device that has not answered yet might be carrying the shim, and offering it 7.8 is the
    // mistake that wipes a phone. A stock device pays for this by waiting one extra round.
    //
    // Policy is looked up through here rather than off the resolved ring, so there is no way to
    // write a query that silently leaves Unknown unprotected.
    public static DeviceRing PolicyRing(this DeviceRing ring) =>
        ring == DeviceRing.Stock ? DeviceRing.Stock : DeviceRing.ReLiveWP;
}
