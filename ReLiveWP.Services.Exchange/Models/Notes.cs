using System.Xml.Serialization;
using ReLiveWP.Services.Exchange.Helpers;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string Notes = "Notes";
}

[XmlRoot("ApplicationData", Namespace = Constants.AirSync)]
public class NoteData
{
    [XmlElement("Subject", Namespace = Constants.Notes)]
    public string? Subject { get; set; }

    [XmlElement("MessageClass", Namespace = Constants.Notes)]
    public string? MessageClass { get; set; }

    [XmlIgnore]
    public DateTime? LastModifiedDate { get; set; }

    [XmlElement("LastModifiedDate", Namespace = Constants.Notes)]
    public string? LastModifiedDateXml
    {
        get => EasDateHelper.FromDateTimeCompact(LastModifiedDate);
        set => LastModifiedDate = EasDateHelper.ToDateTime(value);
    }

    [XmlElement("Categories", Namespace = Constants.Notes)]
    public NoteCategories? Categories { get; set; }

    // airsyncbase:Body holds the note text (v12.0+/WP7).
    [XmlElement("Body", Namespace = Constants.AirSyncBase)]
    public AirSyncBody? Body { get; set; }

    [XmlElement("NativeBodyType", Namespace = Constants.AirSyncBase)]
    public byte? NativeBodyType { get; set; }

    // Conversions: see Extensions/ProtoExtensions.cs for ToProtoNote().
}

// <notes:Categories> container
public class NoteCategories
{
    [XmlElement("Category", Namespace = Constants.Notes)]
    public List<string> Items { get; set; } = [];
}
