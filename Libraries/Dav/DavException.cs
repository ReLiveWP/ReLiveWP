namespace ReLiveWP.Dav;

public class DavException(string message, int? status = null, string? body = null) : Exception(message)
{
    public int? Status { get; } = status;

    public string? Body { get; } = body;
}

public class DavParseException(string message, string? body = null) : DavException(message, null, body);

public class DavSyncTokenException(string message, int? status = null, string? body = null)
    : DavException(message, status, body);
