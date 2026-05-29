using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Data.Entities;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string Contacts = "Contacts";
    public const string Contacts2 = "Contacts2";
}

// Represents the content of an airsync:ApplicationData element for the Contacts class.
// Serialised with XmlSerializer; the root element is used only when round-tripping the
// full document — in Sync responses the child elements are injected directly into
// ApplicationData.Elements.
[XmlRoot("ApplicationData", Namespace = Constants.AirSync)]
public class ContactData
{
    // ── Name ──────────────────────────────────────────────────────────────────
    [XmlElement("FirstName", Namespace = Constants.Contacts)]
    public string? FirstName { get; set; }

    [XmlElement("MiddleName", Namespace = Constants.Contacts)]
    public string? MiddleName { get; set; }

    [XmlElement("LastName", Namespace = Constants.Contacts)]
    public string? LastName { get; set; }

    [XmlElement("Title", Namespace = Constants.Contacts)]
    public string? Title { get; set; }

    [XmlElement("Suffix", Namespace = Constants.Contacts)]
    public string? Suffix { get; set; }

    [XmlElement("FileAs", Namespace = Constants.Contacts)]
    public string? FileAs { get; set; }

    [XmlElement("Alias", Namespace = Constants.Contacts)]
    public string? Alias { get; set; }

    [XmlElement("NickName", Namespace = Constants.Contacts2)]
    public string? NickName { get; set; }

    // ── Yomi ──────────────────────────────────────────────────────────────────
    [XmlElement("YomiFirstName", Namespace = Constants.Contacts)]
    public string? YomiFirstName { get; set; }

    [XmlElement("YomiLastName", Namespace = Constants.Contacts)]
    public string? YomiLastName { get; set; }

    [XmlElement("YomiCompanyName", Namespace = Constants.Contacts)]
    public string? YomiCompanyName { get; set; }

    // ── Organisation ──────────────────────────────────────────────────────────
    [XmlElement("CompanyName", Namespace = Constants.Contacts)]
    public string? CompanyName { get; set; }

    [XmlElement("Department", Namespace = Constants.Contacts)]
    public string? Department { get; set; }

    [XmlElement("JobTitle", Namespace = Constants.Contacts)]
    public string? JobTitle { get; set; }

    [XmlElement("OfficeLocation", Namespace = Constants.Contacts)]
    public string? OfficeLocation { get; set; }

    [XmlElement("AccountName", Namespace = Constants.Contacts2)]
    public string? AccountName { get; set; }

    [XmlElement("ManagerName", Namespace = Constants.Contacts2)]
    public string? ManagerName { get; set; }

    [XmlElement("CustomerId", Namespace = Constants.Contacts2)]
    public string? CustomerId { get; set; }

    [XmlElement("GovernmentId", Namespace = Constants.Contacts2)]
    public string? GovernmentId { get; set; }

    [XmlElement("AssistantName", Namespace = Constants.Contacts)]
    public string? AssistantName { get; set; }

    // ── Email ─────────────────────────────────────────────────────────────────
    [XmlElement("Email1Address", Namespace = Constants.Contacts)]
    public string? Email1Address { get; set; }

    [XmlElement("Email2Address", Namespace = Constants.Contacts)]
    public string? Email2Address { get; set; }

    [XmlElement("Email3Address", Namespace = Constants.Contacts)]
    public string? Email3Address { get; set; }

    // ── Phone numbers ─────────────────────────────────────────────────────────
    [XmlElement("BusinessPhoneNumber", Namespace = Constants.Contacts)]
    public string? BusinessPhoneNumber { get; set; }

    [XmlElement("Business2PhoneNumber", Namespace = Constants.Contacts)]
    public string? Business2PhoneNumber { get; set; }

    [XmlElement("BusinessFaxNumber", Namespace = Constants.Contacts)]
    public string? BusinessFaxNumber { get; set; }

    [XmlElement("HomePhoneNumber", Namespace = Constants.Contacts)]
    public string? HomePhoneNumber { get; set; }

    [XmlElement("Home2PhoneNumber", Namespace = Constants.Contacts)]
    public string? Home2PhoneNumber { get; set; }

    [XmlElement("HomeFaxNumber", Namespace = Constants.Contacts)]
    public string? HomeFaxNumber { get; set; }

    [XmlElement("MobilePhoneNumber", Namespace = Constants.Contacts)]
    public string? MobilePhoneNumber { get; set; }

    [XmlElement("CarPhoneNumber", Namespace = Constants.Contacts)]
    public string? CarPhoneNumber { get; set; }

    [XmlElement("PagerNumber", Namespace = Constants.Contacts)]
    public string? PagerNumber { get; set; }

    [XmlElement("RadioPhoneNumber", Namespace = Constants.Contacts)]
    public string? RadioPhoneNumber { get; set; }

    [XmlElement("AssistantPhoneNumber", Namespace = Constants.Contacts)]
    public string? AssistantPhoneNumber { get; set; }

    [XmlElement("CompanyMainPhone", Namespace = Constants.Contacts2)]
    public string? CompanyMainPhone { get; set; }

    [XmlElement("MMS", Namespace = Constants.Contacts2)]
    public string? MMS { get; set; }

    // ── Instant messaging ─────────────────────────────────────────────────────
    [XmlElement("IMAddress", Namespace = Constants.Contacts2)]
    public string? IMAddress { get; set; }

    [XmlElement("IMAddress2", Namespace = Constants.Contacts2)]
    public string? IMAddress2 { get; set; }

    [XmlElement("IMAddress3", Namespace = Constants.Contacts2)]
    public string? IMAddress3 { get; set; }

    // ── Business address ──────────────────────────────────────────────────────
    [XmlElement("BusinessAddressStreet", Namespace = Constants.Contacts)]
    public string? BusinessAddressStreet { get; set; }

    [XmlElement("BusinessAddressCity", Namespace = Constants.Contacts)]
    public string? BusinessAddressCity { get; set; }

    [XmlElement("BusinessAddressState", Namespace = Constants.Contacts)]
    public string? BusinessAddressState { get; set; }

    [XmlElement("BusinessAddressPostalCode", Namespace = Constants.Contacts)]
    public string? BusinessAddressPostalCode { get; set; }

    [XmlElement("BusinessAddressCountry", Namespace = Constants.Contacts)]
    public string? BusinessAddressCountry { get; set; }

    // ── Home address ──────────────────────────────────────────────────────────
    [XmlElement("HomeAddressStreet", Namespace = Constants.Contacts)]
    public string? HomeAddressStreet { get; set; }

    [XmlElement("HomeAddressCity", Namespace = Constants.Contacts)]
    public string? HomeAddressCity { get; set; }

    [XmlElement("HomeAddressState", Namespace = Constants.Contacts)]
    public string? HomeAddressState { get; set; }

    [XmlElement("HomeAddressPostalCode", Namespace = Constants.Contacts)]
    public string? HomeAddressPostalCode { get; set; }

    [XmlElement("HomeAddressCountry", Namespace = Constants.Contacts)]
    public string? HomeAddressCountry { get; set; }

    // ── Other address ─────────────────────────────────────────────────────────
    [XmlElement("OtherAddressStreet", Namespace = Constants.Contacts)]
    public string? OtherAddressStreet { get; set; }

    [XmlElement("OtherAddressCity", Namespace = Constants.Contacts)]
    public string? OtherAddressCity { get; set; }

    [XmlElement("OtherAddressState", Namespace = Constants.Contacts)]
    public string? OtherAddressState { get; set; }

    [XmlElement("OtherAddressPostalCode", Namespace = Constants.Contacts)]
    public string? OtherAddressPostalCode { get; set; }

    [XmlElement("OtherAddressCountry", Namespace = Constants.Contacts)]
    public string? OtherAddressCountry { get; set; }

    // ── Personal details ──────────────────────────────────────────────────────
    [XmlElement("Spouse", Namespace = Constants.Contacts)]
    public string? Spouse { get; set; }

    [XmlElement("WebPage", Namespace = Constants.Contacts)]
    public string? WebPage { get; set; }

    // Birthday: xs:dateTime on the wire, stored as EAS compact format yyyyMMddTHHmmssZ.
    // The time portion SHOULD be ignored per spec.
    [XmlIgnore]
    public DateTime? Birthday { get; set; }

    [XmlElement("Birthday", Namespace = Constants.Contacts)]
    public string? BirthdayXml
    {
        get => EasDate.FromDateTime(Birthday);
        set => Birthday = EasDate.ToDateTime(value);
    }

    [XmlIgnore]
    public DateTime? Anniversary { get; set; }

    [XmlElement("Anniversary", Namespace = Constants.Contacts)]
    public string? AnniversaryXml
    {
        get => EasDate.FromDateTime(Anniversary);
        set => Anniversary = EasDate.ToDateTime(value);
    }

    // ── Picture ───────────────────────────────────────────────────────────────
    // xs:string on the wire (base64-encoded). XmlSerializer handles byte[] ↔ base64.
    [XmlElement("Picture", Namespace = Constants.Contacts)]
    public byte[]? Picture { get; set; }

    // ── Recipient information cache ───────────────────────────────────────────
    // Read-only; only present in RIC responses.
    [XmlElement("WeightedRank", Namespace = Constants.Contacts)]
    public int? WeightedRank { get; set; }

    // ── Multi-valued collections ──────────────────────────────────────────────
    [XmlElement("Categories", Namespace = Constants.Contacts)]
    public ContactCategories? Categories { get; set; }

    [XmlElement("Children", Namespace = Constants.Contacts)]
    public ContactChildren? Children { get; set; }

    // ── Notes (protocol 12.0+) ────────────────────────────────────────────────
    // airsyncbase:Body is the canonical notes element for all versions >= 12.0.
    [XmlElement("Body", Namespace = Constants.AirSyncBase)]
    public AirSyncBody? Body { get; set; }

    // Present in Sync responses to indicate the native body format stored on the server.
    // xs:unsignedByte: 1=PlainText, 2=HTML, 3=RTF.
    [XmlElement("NativeBodyType", Namespace = Constants.AirSyncBase)]
    public byte? NativeBodyType { get; set; }

    // ── Notes (protocol 2.5 only) ─────────────────────────────────────────────
    // contacts:Body / BodySize / BodyTruncated are only used with protocol 2.5.
    [XmlElement("Body", Namespace = Constants.Contacts)]
    public string? BodyLegacy { get; set; }

    [XmlElement("BodySize", Namespace = Constants.Contacts)]
    public int? BodySize { get; set; }

    [XmlElement("BodyTruncated", Namespace = Constants.Contacts)]
    public byte? BodyTruncated { get; set; }

    [XmlElement("Annotations", Namespace = Constants.WindowsLive)]
    public Annotations? Annotations { get; set; } = null;

    // ── Conversions ───────────────────────────────────────────────────────────

    public static ContactData CreateFrom(ContactItem c)
    {
        var contact = new ContactData()
        {
            FirstName = c.FirstName,
            MiddleName = c.MiddleName,
            LastName = c.LastName,
            Title = c.Title,
            Suffix = c.Suffix,
            FileAs = c.FileAs,
            Alias = c.Alias,
            NickName = c.NickName,
            YomiFirstName = c.YomiFirstName,
            YomiLastName = c.YomiLastName,
            YomiCompanyName = c.YomiCompanyName,
            CompanyName = c.CompanyName,
            Department = c.Department,
            JobTitle = c.JobTitle,
            OfficeLocation = c.OfficeLocation,
            AccountName = c.AccountName,
            ManagerName = c.ManagerName,
            CustomerId = c.CustomerId,
            GovernmentId = c.GovernmentId,
            AssistantName = c.AssistantName,
            Email1Address = c.Email1Address,
            Email2Address = c.Email2Address,
            Email3Address = c.Email3Address,
            BusinessPhoneNumber = c.BusinessPhoneNumber,
            Business2PhoneNumber = c.Business2PhoneNumber,
            BusinessFaxNumber = c.BusinessFaxNumber,
            HomePhoneNumber = c.HomePhoneNumber,
            Home2PhoneNumber = c.Home2PhoneNumber,
            HomeFaxNumber = c.HomeFaxNumber,
            MobilePhoneNumber = c.MobilePhoneNumber,
            CarPhoneNumber = c.CarPhoneNumber,
            PagerNumber = c.PagerNumber,
            RadioPhoneNumber = c.RadioPhoneNumber,
            AssistantPhoneNumber = c.AssistantPhoneNumber,
            CompanyMainPhone = c.CompanyMainPhone,
            MMS = c.MMS,
            IMAddress = c.IMAddress,
            IMAddress2 = c.IMAddress2,
            IMAddress3 = c.IMAddress3,
            BusinessAddressStreet = c.BusinessAddressStreet,
            BusinessAddressCity = c.BusinessAddressCity,
            BusinessAddressState = c.BusinessAddressState,
            BusinessAddressPostalCode = c.BusinessAddressPostalCode,
            BusinessAddressCountry = c.BusinessAddressCountry,
            HomeAddressStreet = c.HomeAddressStreet,
            HomeAddressCity = c.HomeAddressCity,
            HomeAddressState = c.HomeAddressState,
            HomeAddressPostalCode = c.HomeAddressPostalCode,
            HomeAddressCountry = c.HomeAddressCountry,
            OtherAddressStreet = c.OtherAddressStreet,
            OtherAddressCity = c.OtherAddressCity,
            OtherAddressState = c.OtherAddressState,
            OtherAddressPostalCode = c.OtherAddressPostalCode,
            OtherAddressCountry = c.OtherAddressCountry,
            Spouse = c.Spouse,
            Birthday = c.Birthday,
            Anniversary = c.Anniversary,
            WebPage = c.WebPage,
            Picture = c.Picture,
            WeightedRank = c.WeightedRank,
            Body = c.Notes is not null ? new AirSyncBody { Type = BodyType.PlainText, Data = c.Notes } : null,
            Categories = c.Categories.Count > 0
            ? new ContactCategories { Items = c.Categories.Select(x => x.Name).ToList() } : null,
            Children = c.Children.Count > 0
            ? new ContactChildren { Items = c.Children.Select(x => x.Name).ToList() } : null,
            // Live annotations are emitted separately in ApplicationData by ItemSyncService,
            // not via the ContactData DTO serialisation path.
            Annotations = null
        };

        return contact;
    }

    public ContactItem ToEntity(string userId, string collectionId) => new()
    {
        UserId = userId,
        CollectionId = collectionId,
        FirstName = FirstName,
        MiddleName = MiddleName,
        LastName = LastName,
        Title = Title,
        Suffix = Suffix,
        FileAs = FileAs,
        Alias = Alias,
        NickName = NickName,
        YomiFirstName = YomiFirstName,
        YomiLastName = YomiLastName,
        YomiCompanyName = YomiCompanyName,
        CompanyName = CompanyName,
        Department = Department,
        JobTitle = JobTitle,
        OfficeLocation = OfficeLocation,
        AccountName = AccountName,
        ManagerName = ManagerName,
        CustomerId = CustomerId,
        GovernmentId = GovernmentId,
        AssistantName = AssistantName,
        Email1Address = Email1Address,
        Email2Address = Email2Address,
        Email3Address = Email3Address,
        BusinessPhoneNumber = BusinessPhoneNumber,
        Business2PhoneNumber = Business2PhoneNumber,
        BusinessFaxNumber = BusinessFaxNumber,
        HomePhoneNumber = HomePhoneNumber,
        Home2PhoneNumber = Home2PhoneNumber,
        HomeFaxNumber = HomeFaxNumber,
        MobilePhoneNumber = MobilePhoneNumber,
        CarPhoneNumber = CarPhoneNumber,
        PagerNumber = PagerNumber,
        RadioPhoneNumber = RadioPhoneNumber,
        AssistantPhoneNumber = AssistantPhoneNumber,
        CompanyMainPhone = CompanyMainPhone,
        MMS = MMS,
        IMAddress = IMAddress,
        IMAddress2 = IMAddress2,
        IMAddress3 = IMAddress3,
        BusinessAddressStreet = BusinessAddressStreet,
        BusinessAddressCity = BusinessAddressCity,
        BusinessAddressState = BusinessAddressState,
        BusinessAddressPostalCode = BusinessAddressPostalCode,
        BusinessAddressCountry = BusinessAddressCountry,
        HomeAddressStreet = HomeAddressStreet,
        HomeAddressCity = HomeAddressCity,
        HomeAddressState = HomeAddressState,
        HomeAddressPostalCode = HomeAddressPostalCode,
        HomeAddressCountry = HomeAddressCountry,
        OtherAddressStreet = OtherAddressStreet,
        OtherAddressCity = OtherAddressCity,
        OtherAddressState = OtherAddressState,
        OtherAddressPostalCode = OtherAddressPostalCode,
        OtherAddressCountry = OtherAddressCountry,
        Spouse = Spouse,
        Birthday = Birthday,
        Anniversary = Anniversary,
        WebPage = WebPage,
        Picture = Picture,
        Notes = Body?.Data ?? BodyLegacy,
        Categories = Categories?.Items
            .Select(n => new ContactCategory { Id = Guid.NewGuid().ToString("N"), Name = n })
            .ToList() ?? [],
        Children = Children?.Items
            .Select(n => new ContactChild { Id = Guid.NewGuid().ToString("N"), Name = n })
            .ToList() ?? [],
    };
}

// <contacts:Categories> container
public class ContactCategories
{
    [XmlElement("Category", Namespace = Constants.Contacts)]
    public List<string> Items { get; set; } = [];
}

// <contacts:Children> container
public class ContactChildren
{
    [XmlElement("Child", Namespace = Constants.Contacts)]
    public List<string> Items { get; set; } = [];
}

