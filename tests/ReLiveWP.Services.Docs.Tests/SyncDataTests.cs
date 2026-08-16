using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ReLiveWP.Services.Docs.Dav;
using ReLiveWP.Services.Docs.Soap;

namespace ReLiveWP.Services.Docs.Tests;

public class SyncDataTests
{
    private static readonly DateTimeOffset Stamp = new(2015, 10, 21, 7, 28, 0, TimeSpan.Zero);

    private static MultiStatusEntry[] Entries() =>
    [
        new("https://docs.relivewp.net/dav/Documents", "Documents", true, 0, Stamp, Stamp, false, "folder-uid"),
        new("https://docs.relivewp.net/dav/Documents/Budget.xlsx", "Budget.xlsx", false, 4096, Stamp, Stamp, false, "file-uid"),
        new("https://docs.relivewp.net/dav/Documents/Gone.docx", "Gone.docx", false, 0, Stamp, Stamp, false, "", null, true),
    ];

    private static string Render(Action<XmlWriter> write)
    {
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, new XmlWriterSettings { OmitXmlDeclaration = true }))
            write(writer);

        return builder.ToString();
    }

    [Fact]
    public void SyncDataAndPropFindEmitByteIdenticalDocuments()
    {
        var entries = Entries();

        var propFind = Render(w => MultiStatusWriter.Write(w, entries));
        var syncData = Render(w => new SyncData { Entries = [.. entries] }.WriteXml(w));

        Assert.Equal(propFind, syncData);
    }

    [Fact]
    public void SyncDataNestsTheMultistatusUnderItsOwnElement()
    {
        var serializer = new XmlSerializer(typeof(SyncDataHolder));
        var builder = new StringBuilder();

        using (var writer = XmlWriter.Create(builder, new XmlWriterSettings { OmitXmlDeclaration = true }))
            serializer.Serialize(writer, new SyncDataHolder { SyncData = new SyncData { Entries = [.. Entries()] } });

        var xml = builder.ToString();
        var open = xml.IndexOf("<SyncData", StringComparison.Ordinal);
        var multistatus = xml.IndexOf("<D:multistatus", StringComparison.Ordinal);
        var close = xml.IndexOf("</SyncData>", StringComparison.Ordinal);

        Assert.True(open >= 0 && multistatus > open && close > multistatus, xml);
    }

    [XmlRoot("holder")]
    public class SyncDataHolder
    {
        public SyncData? SyncData { get; set; }
    }

    [Fact]
    public void RejectingAStaleTokenStillEmitsAWellFormedEmptyMultistatus()
    {
        var xml = Render(w => new SyncData { Entries = [] }.WriteXml(w));

        Assert.Contains("multistatus", xml);
        Assert.DoesNotContain("<D:response>", xml);
        Assert.Equal(xml, Render(w => MultiStatusWriter.Write(w, [])));
    }

    [Fact]
    public void SyncDataCarriesTombstonesThroughAsNotFound()
    {
        var syncData = Render(w => new SyncData { Entries = [.. Entries()] }.WriteXml(w));

        Assert.Contains("HTTP/1.1 404", syncData);
        Assert.Contains("HTTP/1.1 200 OK", syncData);
    }
}
