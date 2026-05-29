namespace ReLiveWP.Services.Exchange;

internal static class BooleanExtensions
{
    public static byte? ToByte(this bool? b) 
        => b switch { true => (byte)1, false => (byte)0, _ => null };
    public static int? ToInt(this bool? b)
        => b switch { true => (byte)1, false => (byte)0, _ => null };
    public static bool? ToBool(this int? b)
        => b switch { 1 => true, 0 => false, _ => null };
    public static bool? ToBool(this byte? b)
        => b switch { 1 => true, 0 => false, _ => null };
}
