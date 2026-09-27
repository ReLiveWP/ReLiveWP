using System.Globalization;
using System.Xml;

namespace ReLiveWP.Services.Docs.Dav;

public record MultiStatusEntry(
    string Href,
    string DisplayName,
    bool IsFolder,
    long Size,
    DateTimeOffset Created,
    DateTimeOffset Modified,
    bool ReadOnly,
    string ResourceId,
    string? ProgId = null,
    bool Deleted = false);

public static class MultiStatusWriter
{
    public const string DavNamespace = "DAV:";
    public const string ReplNamespace = "http://schemas.microsoft.com/repl/";

    private const string DavPrefix = "D";
    private const string ReplPrefix = "Repl";
    private const string FolderValue = "t";
    private const string NotFolderValue = "f";
    private const string ReadOnlyValue = "1";
    private const string WritableValue = "0";
    private const string FoundStatus = "HTTP/1.1 200 OK";
    private const string DeletedStatus = "HTTP/1.1 404 Not Found";

    public static void Write(XmlWriter writer, IEnumerable<MultiStatusEntry> entries)
    {
        writer.WriteStartElement(DavPrefix, "multistatus", DavNamespace);
        writer.WriteAttributeString("xmlns", ReplPrefix, null, ReplNamespace);

        foreach (var entry in entries)
            WriteResponse(writer, entry);

        writer.WriteEndElement();
    }

    private static void WriteResponse(XmlWriter writer, MultiStatusEntry entry)
    {
        writer.WriteStartElement(DavPrefix, "response", DavNamespace);

        writer.WriteStartElement(DavPrefix, "href", DavNamespace);
        writer.WriteString(entry.Href);
        writer.WriteEndElement();

        writer.WriteStartElement(DavPrefix, "propstat", DavNamespace);

        writer.WriteStartElement(DavPrefix, "prop", DavNamespace);
        WriteProperties(writer, entry);
        writer.WriteEndElement();

        writer.WriteStartElement(DavPrefix, "status", DavNamespace);
        writer.WriteString(entry.Deleted ? DeletedStatus : FoundStatus);
        writer.WriteEndElement();

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteProperties(XmlWriter writer, MultiStatusEntry entry)
    {
        WriteDav(writer, "displayname", entry.DisplayName);
        WriteDav(writer, "isFolder", entry.IsFolder ? FolderValue : NotFolderValue);
        WriteDav(writer, "creationdate", HttpDate(entry.Created));
        WriteDav(writer, "getlastmodified", HttpDate(entry.Modified));
        WriteDav(writer, "isreadonly", entry.ReadOnly ? ReadOnlyValue : WritableValue);

        if (!entry.IsFolder)
            WriteDav(writer, "getcontentlength", entry.Size.ToString(CultureInfo.InvariantCulture));

        writer.WriteStartElement(ReplPrefix, "repl-uid", ReplNamespace);
        writer.WriteString(entry.ResourceId);
        writer.WriteEndElement();

        if (!string.IsNullOrEmpty(entry.ProgId))
        {
            writer.WriteStartElement("progid");
            writer.WriteString(entry.ProgId);
            writer.WriteEndElement();
        }
    }

    private static void WriteDav(XmlWriter writer, string name, string value)
    {
        writer.WriteStartElement(DavPrefix, name, DavNamespace);
        writer.WriteString(value);
        writer.WriteEndElement();
    }

    public static string HttpDate(DateTimeOffset value)
        => value.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
}
