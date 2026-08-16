using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Tests;

/// <summary>
/// The elements a client may name in Supported, transcribed from the GhostingProps groups in
/// MS-ASCNTC (contacts, contacts2) and MS-ASCAL (calendar), plus the required/optional Calendar
/// split in MS-ASCMD 2.2.3.179.
/// </summary>
internal static class GhostableVocabulary
{
    // MS-ASCNTC contacts:GhostingProps. AssistnamePhoneNumber is in the group too but it is the
    // spec's own misspelling of AssistantPhoneNumber and has no code page token, so it is omitted.
    // Picture is in the group but MS-ASCMD 2.2.3.24 pins it as never-cleared, so it lives in
    // NeverCleared rather than here.
    public static readonly string[] Contacts =
    [
        "Anniversary", "AssistantName", "AssistantPhoneNumber", "Birthday",
        "Business2PhoneNumber", "BusinessAddressCity", "BusinessAddressCountry",
        "BusinessAddressPostalCode", "BusinessAddressState", "BusinessAddressStreet",
        "BusinessFaxNumber", "BusinessPhoneNumber", "CarPhoneNumber", "Categories", "Children",
        "CompanyName", "Department", "Email1Address", "Email2Address", "Email3Address", "FileAs",
        "FirstName", "Home2PhoneNumber", "HomeAddressCity", "HomeAddressCountry",
        "HomeAddressPostalCode", "HomeAddressState", "HomeAddressStreet", "HomeFaxNumber",
        "HomePhoneNumber", "JobTitle", "LastName", "MiddleName", "MobilePhoneNumber",
        "OfficeLocation", "OtherAddressCity", "OtherAddressCountry", "OtherAddressPostalCode",
        "OtherAddressState", "OtherAddressStreet", "PagerNumber", "RadioPhoneNumber", "Spouse",
        "Suffix", "Title", "WebPage", "YomiCompanyName", "YomiFirstName", "YomiLastName",
    ];

    // MS-ASCNTC contacts2:GhostingProps, all ten of them. These arrive on their own code page and
    // so their own namespace, which is what the clear pass used to get wrong.
    public static readonly string[] Contacts2 =
    [
        "AccountName", "CompanyMainPhone", "CustomerId", "GovernmentId", "IMAddress",
        "IMAddress2", "IMAddress3", "MMS", "ManagerName", "NickName",
    ];

    // MS-ASCMD 2.2.3.179: fourteen required, then six optional
    public static readonly string[] Calendar =
    [
        "DtStamp", "Categories", "Sensitivity", "BusyStatus", "UID", "Timezone", "StartTime",
        "Subject", "Location", "EndTime", "Recurrence", "AllDayEvent", "Reminder", "Exceptions",
        "Attendees", "OrganizerName", "OrganizerEmail", "MeetingStatus", "ResponseRequested",
        "DisallowNewTimeProposal",
    ];

    public static IEnumerable<(string Ns, string Name)> All()
    {
        foreach (var n in Contacts) yield return (Constants.Contacts, n);
        foreach (var n in Contacts2) yield return (Constants.Contacts2, n);
        foreach (var n in Calendar) yield return (Constants.Calendar, n);
    }

    public static TheoryData<string, string> ContactRows()
    {
        var data = new TheoryData<string, string>();
        foreach (var n in Contacts) data.Add(Constants.Contacts, n);
        foreach (var n in Contacts2) data.Add(Constants.Contacts2, n);
        return data;
    }

    public static TheoryData<string, string> CalendarRows()
    {
        var data = new TheoryData<string, string>();
        foreach (var n in Calendar) data.Add(Constants.Calendar, n);
        return data;
    }
}
