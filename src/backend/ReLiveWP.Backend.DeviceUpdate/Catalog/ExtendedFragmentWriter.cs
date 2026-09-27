using System.Security.Cryptography;
using System.Xml.Linq;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Catalog;

// Builds the GetExtendedUpdateInfo blob: ExtendedProperties, Files and HandlerSpecificData as
// siblings, then one LocalizedProperties per language. Written as one blob rather than as fragment
// rows so ExtendedFragmentReader splits it, which is the same path a crawled revision takes.
public static class ExtendedFragmentWriter
{
    private const string CommandLineHandler = "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/CommandLineInstallation";

    public static string Write(ManifestUpdate update, IReadOnlyList<ParsedFile> files)
    {
        var parts = new List<XElement> { WriteExtendedProperties(update, files) };

        if (files.Count > 0)
            parts.Add(new XElement("Files", files.Select(WriteFile)));

        if (update.InstallCommand is not null)
            parts.Add(WriteHandlerSpecificData(update.InstallCommand));

        parts.AddRange(update.Localizations.Select(WriteLocalizedProperties));

        return string.Concat(parts.Select(p => p.ToString(SaveOptions.DisableFormatting)));
    }

    public static ParsedFile ReadFile(string path, string? sourceUrl)
    {
        var info = new FileInfo(path);
        using var stream = File.OpenRead(path);

        var sha1 = SHA1.HashData(stream);
        stream.Position = 0;
        var sha256 = SHA256.HashData(stream);

        return new ParsedFile
        {
            FileName = info.Name,
            Size = info.Length,
            DigestSha1 = Convert.ToBase64String(sha1),
            DigestSha256 = Convert.ToBase64String(sha256),
            Modified = info.LastWriteTimeUtc,
            SourceUrl = sourceUrl,
        };
    }

    private static XElement WriteExtendedProperties(ManifestUpdate update, IReadOnlyList<ParsedFile> files)
    {
        var language = update.Localizations.FirstOrDefault()?.Language ?? "en";
        var properties = new XElement("ExtendedProperties",
            new XAttribute("DefaultPropertiesLanguage", language));

        if (update.InstallCommand is not null)
            properties.Add(new XAttribute("Handler", CommandLineHandler));

        if (files.Count > 0)
        {
            properties.Add(new XAttribute("MaxDownloadSize", files.Sum(f => f.Size)));
            properties.Add(new XAttribute("MinDownloadSize", 0));
        }

        properties.Add(new XElement("InstallationBehavior"));
        return properties;
    }

    private static XElement WriteFile(ParsedFile file)
    {
        var element = new XElement("File",
            new XAttribute("Digest", file.DigestSha1!),
            new XAttribute("DigestAlgorithm", "SHA1"),
            new XAttribute("FileName", file.FileName),
            new XAttribute("Size", file.Size));

        if (file.Modified is { } modified)
            element.Add(new XAttribute("Modified", modified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));

        if (file.DigestSha256 is { } sha256)
            element.Add(new XElement("AdditionalDigest", new XAttribute("Algorithm", "SHA256"), sha256));

        return element;
    }

    private static XElement WriteHandlerSpecificData(ManifestInstallCommand command)
    {
        var install = new XElement("InstallCommand", new XAttribute("Program", command.Program));

        if (command.Arguments is { } arguments)
            install.Add(new XAttribute("Arguments", arguments));

        install.Add(
            new XAttribute("DefaultResult", command.DefaultResult),
            new XAttribute("RebootByDefault", command.RebootByDefault ? "true" : "false"),
            new XElement("ReturnCode",
                new XAttribute("Code", 0),
                new XAttribute("Result", "Failed"),
                new XAttribute("Reboot", "false")));

        return new XElement("HandlerSpecificData",
            new XAttribute("type", "cmd:CommandLineInstallation"),
            install);
    }

    private static XElement WriteLocalizedProperties(ManifestLocalization localization)
    {
        var properties = new XElement("LocalizedProperties",
            new XElement("Language", localization.Language));

        if (localization.Title is { } title)
            properties.Add(new XElement("Title", title));

        if (localization.Description is { } description)
            properties.Add(new XElement("Description", description));

        if (localization.MoreInfoUrl is { } moreInfo)
            properties.Add(new XElement("MoreInfoUrl", moreInfo));

        if (localization.SupportUrl is { } support)
            properties.Add(new XElement("SupportUrl", support));

        return properties;
    }
}
