using System.Buffers.Binary;
using System.Text;

namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror.Calendar;

// MS-ASDTYPE 2.7.6. 172 bytes, base64 into calendar:Timezone.
public static class EasTimeZone
{
    public const int Size = 172;

    private const int NameChars = 32;

    private const int BiasOffset = 0;
    private const int StandardNameOffset = 4;
    private const int StandardDateOffset = 68;
    private const int StandardBiasOffset = 84;
    private const int DaylightNameOffset = 88;
    private const int DaylightDateOffset = 152;
    private const int DaylightBiasOffset = 168;

    public static string ToBase64(TimeZoneInfo zone, DateTime forInstantUtc) =>
        Convert.ToBase64String(Build(zone, forInstantUtc));

    public static byte[] Build(TimeZoneInfo zone, DateTime forInstantUtc)
    {
        var buffer = new byte[Size];

        WriteInt32(buffer, BiasOffset, -(int)zone.BaseUtcOffset.TotalMinutes);

        WriteName(buffer, StandardNameOffset, zone.StandardName);
        WriteName(buffer, DaylightNameOffset, zone.DaylightName);

        // standard time is the base offset, so the extra minutes all live on the daylight side
        WriteInt32(buffer, StandardBiasOffset, 0);

        if (RuleFor(zone, forInstantUtc) is not { } rule)
        {
            // a zone with no DST leaves both transitions zeroed, which is how wMonth 0 reads
            WriteInt32(buffer, DaylightBiasOffset, 0);
            return buffer;
        }

        WriteInt32(buffer, DaylightBiasOffset, -(int)rule.DaylightDelta.TotalMinutes);

        WriteTransition(buffer, DaylightDateOffset, rule.DaylightTransitionStart);
        WriteTransition(buffer, StandardDateOffset, rule.DaylightTransitionEnd);

        return buffer;
    }

    private static TimeZoneInfo.AdjustmentRule? RuleFor(TimeZoneInfo zone, DateTime forInstantUtc)
    {
        var rules = zone.GetAdjustmentRules();
        if (rules.Length == 0) return null;

        var local = TimeZoneInfo.ConvertTimeFromUtc(forInstantUtc, zone).Date;

        return rules.FirstOrDefault(r => r.DateStart <= local && local <= r.DateEnd) ?? rules[^1];
    }

    private static void WriteTransition(byte[] buffer, int offset, TimeZoneInfo.TransitionTime transition)
    {
        var time = transition.TimeOfDay;

        WriteUInt16(buffer, offset + 0, (ushort)(transition.IsFixedDateRule ? 1 : 0));
        WriteUInt16(buffer, offset + 2, (ushort)transition.Month);
        WriteUInt16(buffer, offset + 4, (ushort)(transition.IsFixedDateRule ? 0 : (int)transition.DayOfWeek));
        WriteUInt16(buffer, offset + 6, (ushort)(transition.IsFixedDateRule ? transition.Day : transition.Week));
        WriteUInt16(buffer, offset + 8, (ushort)time.Hour);
        WriteUInt16(buffer, offset + 10, (ushort)time.Minute);
        WriteUInt16(buffer, offset + 12, (ushort)time.Second);
        WriteUInt16(buffer, offset + 14, (ushort)time.Millisecond);
    }

    private static void WriteName(byte[] buffer, int offset, string name)
    {
        // null terminaed
        var trimmed = name.Length > NameChars - 1 ? name[..(NameChars - 1)] : name;
        Encoding.Unicode.GetBytes(trimmed, 0, trimmed.Length, buffer, offset);
    }

    private static void WriteInt32(byte[] buffer, int offset, int value) =>
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, sizeof(int)), value);

    private static void WriteUInt16(byte[] buffer, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset, sizeof(ushort)), value);
}
