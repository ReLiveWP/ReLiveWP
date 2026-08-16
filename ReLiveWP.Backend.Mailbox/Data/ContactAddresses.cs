using ReLiveWP.Backend.Mailbox.Data.Entities;

namespace ReLiveWP.Backend.Mailbox.Data;

public static class ContactAddresses
{
    public static IEnumerable<string> Of(DbContactItem contact) =>
        new[] { contact.Email1Address, contact.Email2Address, contact.Email3Address }
            .Select(Normalize)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal);

    public static bool IsMeContact(DbContactItem contact) =>
        string.Equals(contact.Annotation?.ContactType, "Me", StringComparison.OrdinalIgnoreCase);

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var address = value.Trim();

        var open = address.LastIndexOf('<');
        if (open >= 0)
        {
            var close = address.IndexOf('>', open + 1);
            if (close > open + 1)
                address = address[(open + 1)..close].Trim();
        }

        return address.Length == 0 ? null : address.ToUpperInvariant();
    }
}
