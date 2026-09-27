namespace ReLiveWP.Backend.DeviceUpdate.Model;

// Which deployment policy a device gets. There is no device identity to key this on: no
// registration, the cookie is a random GUID we never validate, and the traffic logger only knows an
// IP. The only thing a device tells us about itself is which revisions it reports, so the ring is
// derived from that and nothing else.
public enum DeviceRing
{
    // Has not evaluated the marker yet, so we cannot tell. Treated as ReLiveWP everywhere it
    // matters, because the expensive mistake is offering 7.8 to a device carrying our shim.
    Unknown,

    Stock,
    ReLiveWP
}
