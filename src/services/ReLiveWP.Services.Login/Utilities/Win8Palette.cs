namespace ReLiveWP.Services.Login.Utilities;

public static class Win8Palette
{
    public static IReadOnlyList<string> Parse(string? win8Colors)
    {
        if (string.IsNullOrWhiteSpace(win8Colors))
            return [];

        if (!Convert.TryFromBase64String(win8Colors, new byte[win8Colors.Length], out _))
            return [];

        var bytes = Convert.FromBase64String(win8Colors);
        var colours = new string[bytes.Length / 4];
        for (var i = 0; i < colours.Length; i++)
        {
            var quad = bytes.AsSpan(i * 4, 4);
            colours[i] = $"#{quad[0]:x2}{quad[1]:x2}{quad[2]:x2}";
        }

        return colours;
    }
}
