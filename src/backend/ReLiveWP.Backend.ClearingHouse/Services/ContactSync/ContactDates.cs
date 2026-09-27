using ReLiveWP.ServiceDefaults.Contacts;

namespace ReLiveWP.Backend.ClearingHouse.Services.ContactSync;

public static class ContactDates
{
    public const int UnknownYear = ContactContract.UnknownBirthYear;

    public static DateTime Clamp(DateTime date) => ContactContract.ClampBirthYear(date);
}
