using System.Security.Cryptography;
using System.Text;

namespace ReLiveWP.Identity;

// CID synthesis for the People-hub contact-anchored model. Two disjoint derivations:
//   - ContactCid: a contact's stable faux CID, scoped per-user so the same identity in two
//     address books never collides. Inert until an external identity links to it.
//   - SynthesiseAuthorCid: a standalone CID for an unlinked public author. Global (public data),
//     never coincides with a ContactCid, so it can never bind as someone's contact.
// Both fold SHA-1 to a non-negative int64, matching the signed-decimal CID the device parses
// with %lld.
public static class Cids
{
    public static long ContactCid(string userId, string key)
        => Fold(userId + ":" + key);

    public static long SynthesiseAuthorCid(string provider, string externalId)
        => Fold(provider + ":" + externalId);

    private static long Fold(string value)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToInt64(hash, 0) & long.MaxValue;
    }
}
