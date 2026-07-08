using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string WindowsLive = "WindowsLive";
    public const string FolderHierarchy = "FolderHierarchy";
}

public enum FolderType
{
    Generic = 1,
    InboxDefault,
    DraftsDefault,
    DeletedItemsDefault,
    SentItemsDefault,
    OutboxDefault,
    TasksDefault,
    CalendarDefault,
    ContactsDefault,
    NotesDefault,
    JournalDefault,
    Mail,
    Calendar,
    Contacts,
    Task,
    Journal,
    Notes,
    Unknown,
    RecipientInformationCache, // ??
    MeContact = 27
}

[XmlRoot("FolderSync", Namespace = Constants.FolderHierarchy)]
public class FolderSync
{
    [XmlElement("Status", Namespace = Constants.FolderHierarchy)]
    public int Status { get; set; } = 1;

    [XmlElement("SyncKey", Namespace = Constants.FolderHierarchy)]
    public string SyncKey { get; set; }

    // Present in response when client subscribed to FolderSync-level annotations
    [XmlElement("Annotations", Namespace = Constants.WindowsLive)]
    public Annotations? Annotations { get; set; }
    public bool ShouldSerializeAnnotations() => Annotations?.Items is { Count: > 0 };

    // Appears only in some responses
    [XmlElement("Changes", Namespace = Constants.FolderHierarchy)]
    public Changes? Changes { get; set; }
}

// FolderHierarchy block
public class Changes
{
    [XmlElement("Count", Namespace = Constants.FolderHierarchy)]
    public int Count { get; set; }

    [XmlElement("Add", Namespace = Constants.FolderHierarchy)]
    public List<FolderChange> Add { get; set; } = [];

    [XmlElement("Update", Namespace = Constants.FolderHierarchy)]
    public List<FolderChange> Update { get; set; } = [];

    [XmlElement("Delete", Namespace = Constants.FolderHierarchy)]
    public List<FolderChange> Delete { get; set; } = [];
}

public class FolderChange
{
    [XmlElement("ServerId", Namespace = Constants.FolderHierarchy)]
    public string ServerId { get; set; }

    [XmlElement("ParentId", Namespace = Constants.FolderHierarchy)]
    public string? ParentId { get; set; }

    [XmlElement("DisplayName", Namespace = Constants.FolderHierarchy)]
    public string DisplayName { get; set; }

    [XmlIgnore()]
    public FolderType Type { get; set; }

    [XmlElement("Type", Namespace = Constants.FolderHierarchy)]
    public int TypeInt
    {
        get
        {
            return (int)Type;
        }
        set
        {
            Type = (FolderType)value;
        }
    }

    // A Delete carries only ServerId; suppress the (unset, 0) Type element for deletes.
    public bool ShouldSerializeTypeInt() => Type != default;

    [XmlElement("Annotations", Namespace = Constants.WindowsLive)]
    public Annotations? Annotations { get; set; }
    public bool ShouldSerializeAnnotations() => Annotations?.Items is { Count: > 0 };
}