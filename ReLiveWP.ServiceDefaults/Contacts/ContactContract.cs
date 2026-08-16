namespace ReLiveWP.ServiceDefaults.Contacts;

public static class ContactContract
{
    public const int UnknownBirthYear = 1908;
    public const int MaxPictureBase64Bytes = 48 * 1024;
    public const int MaxPictureBytes = MaxPictureBase64Bytes / 4 * 3;

    public static DateTime ClampBirthYear(DateTime date) =>
        date.Year < UnknownBirthYear
            ? new DateTime(UnknownBirthYear, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind)
            : date;
}

// N.B. keep in sync with constants in ConnectedServices
public static class ConnectionConsts
{
    public const ulong TransientFlag = 2;
    public const ulong BustedFlag = 0x80000000;

    public const uint ContactsCapability = 0x2;
    public const uint CalendarCapability = 0x4;
}
