namespace ReLiveWP.Services.Exchange.Data.Entities;

// ── Contact (code pages 1 + 12) ──────────────────────────────────────────────
// All scalar fields map directly to nullable columns in the TPH Items table.
// Multi-valued elements (Categories/Children) use separate tables.

public class ContactItem : Item
{
    // ── Name ─────────────────────────────────────────────────────────────────
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? Title { get; set; }          // salutation/prefix (e.g. "Dr.")
    public string? Suffix { get; set; }
    public string? FileAs { get; set; }         // display/filing string
    public string? Alias { get; set; }          // RIC only; read-only
    public string? NickName { get; set; }       // contacts2

    // ── Yomi (Japanese phonetic) ──────────────────────────────────────────────
    public string? YomiFirstName { get; set; }
    public string? YomiLastName { get; set; }
    public string? YomiCompanyName { get; set; }

    // ── Organisation ─────────────────────────────────────────────────────────
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? OfficeLocation { get; set; }
    public string? AccountName { get; set; }    // contacts2
    public string? ManagerName { get; set; }    // contacts2
    public string? CustomerId { get; set; }     // contacts2
    public string? GovernmentId { get; set; }   // contacts2
    public string? AssistantName { get; set; }

    // ── Email ─────────────────────────────────────────────────────────────────
    public string? Email1Address { get; set; }
    public string? Email2Address { get; set; }
    public string? Email3Address { get; set; }

    // ── Phone numbers ─────────────────────────────────────────────────────────
    public string? BusinessPhoneNumber { get; set; }
    public string? Business2PhoneNumber { get; set; }
    public string? BusinessFaxNumber { get; set; }
    public string? HomePhoneNumber { get; set; }
    public string? Home2PhoneNumber { get; set; }
    public string? HomeFaxNumber { get; set; }
    public string? MobilePhoneNumber { get; set; }
    public string? CarPhoneNumber { get; set; }
    public string? PagerNumber { get; set; }
    public string? RadioPhoneNumber { get; set; }
    public string? AssistantPhoneNumber { get; set; }
    public string? CompanyMainPhone { get; set; }   // contacts2
    public string? MMS { get; set; }                // contacts2

    // ── Instant messaging ─────────────────────────────────────────────────────
    public string? IMAddress { get; set; }      // contacts2
    public string? IMAddress2 { get; set; }     // contacts2
    public string? IMAddress3 { get; set; }     // contacts2

    // ── Business address ──────────────────────────────────────────────────────
    public string? BusinessAddressStreet { get; set; }
    public string? BusinessAddressCity { get; set; }
    public string? BusinessAddressState { get; set; }
    public string? BusinessAddressPostalCode { get; set; }
    public string? BusinessAddressCountry { get; set; }

    // ── Home address ──────────────────────────────────────────────────────────
    public string? HomeAddressStreet { get; set; }
    public string? HomeAddressCity { get; set; }
    public string? HomeAddressState { get; set; }
    public string? HomeAddressPostalCode { get; set; }
    public string? HomeAddressCountry { get; set; }

    // ── Other address ─────────────────────────────────────────────────────────
    public string? OtherAddressStreet { get; set; }
    public string? OtherAddressCity { get; set; }
    public string? OtherAddressState { get; set; }
    public string? OtherAddressPostalCode { get; set; }
    public string? OtherAddressCountry { get; set; }

    // ── Personal details ──────────────────────────────────────────────────────
    public string? Spouse { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime? Anniversary { get; set; }
    public string? WebPage { get; set; }

    // ── Notes (contacts:Body / airsyncbase:Body) ──────────────────────────────
    // Stored as plain text. The protocol wrapper (BodySize, BodyTruncated) is
    // computed at serialisation time and is not persisted.
    public string? Notes { get; set; }

    // ── Picture ───────────────────────────────────────────────────────────────
    // Stored as raw bytes (max ~36 KB per spec). Base64-encoded in XML.
    public byte[]? Picture { get; set; }

    // ── Recipient information cache rank ──────────────────────────────────────
    // Only returned in RIC responses; read-only from the client's perspective.
    public int? WeightedRank { get; set; }

    // ── Multi-valued ──────────────────────────────────────────────────────────
    public List<ContactCategory> Categories { get; set; } = [];
    public List<ContactChild> Children { get; set; } = [];

    // ── Windows Live annotations (optional, loaded on demand) ─────────────────
    public ContactAnnotation? Annotation { get; set; }
}

public class ContactCategory
{
    public string Id { get; set; }
    public string ContactItemId { get; set; }
    public ContactItem ContactItem { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class ContactChild
{
    public string Id { get; set; }
    public string ContactItemId { get; set; }
    public ContactItem ContactItem { get; set; } = null!;
    public string Name { get; set; } = null!;
}
