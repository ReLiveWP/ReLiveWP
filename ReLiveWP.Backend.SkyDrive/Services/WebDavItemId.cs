using System.Text;

namespace ReLiveWP.Backend.SkyDrive.Services;

public static class WebDavItemId
{
    public static string Encode(string path)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(path))
                  .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static bool TryDecode(string itemId, out string path)
    {
        path = "";

        if (string.IsNullOrEmpty(itemId))
            return false;

        var padded = itemId.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(padded));

            if (decoded.Length == 0 || decoded.Contains("..", StringComparison.Ordinal) || decoded.StartsWith('/'))
                return false;

            path = decoded;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
