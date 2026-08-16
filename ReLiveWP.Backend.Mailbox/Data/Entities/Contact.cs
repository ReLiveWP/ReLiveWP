namespace ReLiveWP.Backend.Mailbox.Data.Entities;

public class DbContactItem : DbItem
{
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? Title { get; set; }
    public string? Suffix { get; set; }
    public string? FileAs { get; set; }
    public string? Alias { get; set; }
    public string? NickName { get; set; }

    public string? YomiFirstName { get; set; }
    public string? YomiLastName { get; set; }
    public string? YomiCompanyName { get; set; }

    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? OfficeLocation { get; set; }
    public string? AccountName { get; set; }
    public string? ManagerName { get; set; }
    public string? CustomerId { get; set; }
    public string? GovernmentId { get; set; }
    public string? AssistantName { get; set; }

    public string? Email1Address { get; set; }
    public string? Email2Address { get; set; }
    public string? Email3Address { get; set; }

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
    public string? CompanyMainPhone { get; set; }
    public string? MMS { get; set; }

    public string? IMAddress { get; set; }
    public string? IMAddress2 { get; set; }
    public string? IMAddress3 { get; set; }

    public string? BusinessAddressStreet { get; set; }
    public string? BusinessAddressCity { get; set; }
    public string? BusinessAddressState { get; set; }
    public string? BusinessAddressPostalCode { get; set; }
    public string? BusinessAddressCountry { get; set; }

    public string? HomeAddressStreet { get; set; }
    public string? HomeAddressCity { get; set; }
    public string? HomeAddressState { get; set; }
    public string? HomeAddressPostalCode { get; set; }
    public string? HomeAddressCountry { get; set; }

    public string? OtherAddressStreet { get; set; }
    public string? OtherAddressCity { get; set; }
    public string? OtherAddressState { get; set; }
    public string? OtherAddressPostalCode { get; set; }
    public string? OtherAddressCountry { get; set; }

    public string? Spouse { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime? Anniversary { get; set; }
    public string? WebPage { get; set; }
    public string? Notes { get; set; }
    public byte[]? Picture { get; set; }
    public int? WeightedRank { get; set; }

    public List<DbContactCategory> Categories { get; set; } = [];
    public List<DbContactChild> Children { get; set; } = [];

    public DbContactAnnotation? Annotation { get; set; }
}

public class DbContactCategory
{
    public string Id { get; set; } = null!;
    public string ContactItemId { get; set; } = null!;
    public DbContactItem ContactItem { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class DbContactChild
{
    public string Id { get; set; } = null!;
    public string ContactItemId { get; set; } = null!;
    public DbContactItem ContactItem { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class DbContactAnnotation
{
    public string ContactItemId { get; set; } = null!;
    public DbContactItem ContactItem { get; set; } = null!;

    public long? Cid { get; set; }
    public string? ObjectId { get; set; }
    public string? WLId { get; set; }
    public string? ImMri { get; set; }
    public string? ContactType { get; set; }
    public string? UserTileUrl { get; set; }
    public string? UserTileHash { get; set; }
    public int? TrustLevel { get; set; }
    public int? FavoriteOrder { get; set; }
    public bool LinkIsManual { get; set; }
}

// one row per email address on a contact, so a live user can be found from an address without
// scanning every contact in every mailbox
public class DbContactEmail
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string ContactItemId { get; set; } = null!;
    public DbContactItem ContactItem { get; set; } = null!;
    public string NormalizedAddress { get; set; } = null!;
}

// links an external social identity (provider, external_id) to a contact; unique per (UserId, Provider, ExternalId)
public class DbContactIdentity
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string ContactItemId { get; set; } = null!;
    public DbContactItem ContactItem { get; set; } = null!;
    public long ContactCid { get; set; }
    public string Provider { get; set; } = null!;
    public string ExternalId { get; set; } = null!;
}
