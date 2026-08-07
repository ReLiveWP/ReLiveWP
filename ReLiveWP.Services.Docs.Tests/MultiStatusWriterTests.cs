using System.Text;
using System.Xml;
using System.Xml.Linq;
using ReLiveWP.Services.Docs.Dav;

namespace ReLiveWP.Services.Docs.Tests;

public class MultiStatusWriterTests
{
    private static readonly XNamespace Dav = MultiStatusWriter.DavNamespace;
    private static readonly XNamespace Repl = MultiStatusWriter.ReplNamespace;

    private static readonly DateTimeOffset Stamp =
        new(2015, 10, 21, 7, 28, 0, TimeSpan.Zero);

    private static MultiStatusEntry Folder(string name = "Documents") => new(
        $"https://docs.relivewp.net/dav/{name}", name, true, 0, Stamp, Stamp, false, "folder-uid");

    private static MultiStatusEntry File(string name = "Budget.xlsx") => new(
        $"https://docs.relivewp.net/dav/Documents/{name}", name, false, 4096, Stamp, Stamp, false, "file-uid");

    private static string Write(params MultiStatusEntry[] entries)
    {
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, new XmlWriterSettings { OmitXmlDeclaration = true }))
            MultiStatusWriter.Write(writer, entries);

        return builder.ToString();
    }

    private static XElement Prop(string xml)
        => XDocument.Parse(xml).Root!.Element(Dav + "response")!.Element(Dav + "propstat")!.Element(Dav + "prop")!;

    [Fact]
    public void FolderIsMarkedWithLowercaseT()
    {
        var prop = Prop(Write(Folder()));

        Assert.Equal("t", prop.Element(Dav + "isFolder")!.Value);
    }

    [Fact]
    public void FileIsNotMarkedAsFolder()
    {
        var prop = Prop(Write(File()));

        Assert.NotEqual("t", prop.Element(Dav + "isFolder")!.Value);
    }

    [Fact]
    public void DatesAreRfc1123WithEnglishMonthAndGmt()
    {
        var prop = Prop(Write(File()));

        Assert.Equal("Wed, 21 Oct 2015 07:28:00 GMT", prop.Element(Dav + "creationdate")!.Value);
        Assert.Equal("Wed, 21 Oct 2015 07:28:00 GMT", prop.Element(Dav + "getlastmodified")!.Value);
    }

    [Fact]
    public void DatesSplitIntoExactlySixSpaceSeparatedParts()
    {
        var prop = Prop(Write(File()));

        foreach (var name in new[] { "creationdate", "getlastmodified" })
        {
            var parts = prop.Element(Dav + name)!.Value.Split(' ');
            Assert.Equal(6, parts.Length);
            Assert.Equal(3, parts[4].Split(':').Length);
        }
    }

    [Fact]
    public void NonUtcInputIsConvertedBeforeFormatting()
    {
        var local = new DateTimeOffset(2015, 10, 21, 9, 28, 0, TimeSpan.FromHours(2));
        var entry = File() with { Created = local, Modified = local };

        var prop = Prop(Write(entry));

        Assert.Equal("Wed, 21 Oct 2015 07:28:00 GMT", prop.Element(Dav + "creationdate")!.Value);
    }

    [Fact]
    public void ReadOnlyUsesOneAndZero()
    {
        Assert.Equal("1", Prop(Write(File() with { ReadOnly = true })).Element(Dav + "isreadonly")!.Value);
        Assert.Equal("0", Prop(Write(File() with { ReadOnly = false })).Element(Dav + "isreadonly")!.Value);
    }

    [Fact]
    public void ContentLengthIsPlainDecimalOnFilesOnly()
    {
        Assert.Equal("4096", Prop(Write(File())).Element(Dav + "getcontentlength")!.Value);
        Assert.Null(Prop(Write(Folder())).Element(Dav + "getcontentlength"));
    }

    [Fact]
    public void ReplUidUsesTheReplNamespace()
    {
        var prop = Prop(Write(File()));

        Assert.Equal("file-uid", prop.Element(Repl + "repl-uid")!.Value);
    }

    [Fact]
    public void ProgIdIsUnprefixedAndOmittedWhenEmpty()
    {
        var notebook = Folder("Notebook") with { ProgId = "onenote.notebook" };
        var progId = Prop(Write(notebook)).Element("progid");

        Assert.NotNull(progId);
        Assert.Equal("onenote.notebook", progId!.Value);
        Assert.Equal("", progId.Name.NamespaceName);

        Assert.Null(Prop(Write(Folder())).Element("progid"));
    }

    [Fact]
    public void PrefixesAreLiterallyDAndRepl()
    {
        var xml = Write(File());

        Assert.Contains("<D:multistatus", xml);
        Assert.Contains("<D:response>", xml);
        Assert.Contains("<D:propstat>", xml);
        Assert.Contains("<D:prop>", xml);
        Assert.Contains("<D:href>", xml);
        Assert.Contains("<D:status>", xml);
        Assert.Contains("<Repl:repl-uid>", xml);
        Assert.Contains("<progid>", Write(Folder("N") with { ProgId = "onenote.notebook" }));
    }

    [Fact]
    public void PresentEntriesUse200Status()
    {
        var status = XDocument.Parse(Write(File())).Root!
            .Element(Dav + "response")!.Element(Dav + "propstat")!.Element(Dav + "status")!.Value;

        Assert.Equal("HTTP/1.1 200 OK", status);
    }

    [Fact]
    public void DeletedEntriesUse404Status()
    {
        var deleted = File() with { Deleted = true };
        var status = XDocument.Parse(Write(deleted)).Root!
            .Element(Dav + "response")!.Element(Dav + "propstat")!.Element(Dav + "status")!.Value;

        Assert.StartsWith("HTTP/1.1 404", status);
        Assert.Contains("HTTP/1.1 404", status);
    }

    [Fact]
    public void EveryPropertyTheClientReadsIsPresentOnAFile()
    {
        var prop = Prop(Write(File()));

        Assert.NotNull(prop.Element(Dav + "displayname"));
        Assert.NotNull(prop.Element(Dav + "isFolder"));
        Assert.NotNull(prop.Element(Dav + "creationdate"));
        Assert.NotNull(prop.Element(Dav + "getlastmodified"));
        Assert.NotNull(prop.Element(Dav + "isreadonly"));
        Assert.NotNull(prop.Element(Dav + "getcontentlength"));
        Assert.NotNull(prop.Element(Repl + "repl-uid"));
    }

    [Fact]
    public void ResourceTypeIsNotUsed()
    {
        Assert.Null(Prop(Write(Folder())).Element(Dav + "resourcetype"));
    }
}
