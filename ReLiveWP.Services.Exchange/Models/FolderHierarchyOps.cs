using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

[XmlRoot("FolderCreate", Namespace = Constants.FolderHierarchy)]
public class FolderCreate
{
    [XmlElement("SyncKey", Namespace = Constants.FolderHierarchy)]
    public string? SyncKey { get; set; }

    [XmlElement("ParentId", Namespace = Constants.FolderHierarchy)]
    public string? ParentId { get; set; }

    [XmlElement("DisplayName", Namespace = Constants.FolderHierarchy)]
    public string? DisplayName { get; set; }

    [XmlIgnore()]
    public FolderType Type { get; set; }

    [XmlElement("Type", Namespace = Constants.FolderHierarchy)]
    public int TypeInt
    {
        get => (int)Type;
        set => Type = (FolderType)value;
    }
}

[XmlRoot("FolderCreate", Namespace = Constants.FolderHierarchy)]
public class FolderCreateResponse
{
    [XmlElement("Status", Namespace = Constants.FolderHierarchy)]
    public int Status { get; set; } = 1;

    // must be absent unless the create succeeded
    [XmlElement("SyncKey", Namespace = Constants.FolderHierarchy)]
    public string? SyncKey { get; set; }
    public bool ShouldSerializeSyncKey() => SyncKey is not null;

    [XmlElement("ServerId", Namespace = Constants.FolderHierarchy)]
    public string? ServerId { get; set; }
    public bool ShouldSerializeServerId() => ServerId is not null;
}

[XmlRoot("FolderUpdate", Namespace = Constants.FolderHierarchy)]
public class FolderUpdate
{
    [XmlElement("SyncKey", Namespace = Constants.FolderHierarchy)]
    public string? SyncKey { get; set; }

    [XmlElement("ServerId", Namespace = Constants.FolderHierarchy)]
    public string? ServerId { get; set; }

    [XmlElement("ParentId", Namespace = Constants.FolderHierarchy)]
    public string? ParentId { get; set; }

    [XmlElement("DisplayName", Namespace = Constants.FolderHierarchy)]
    public string? DisplayName { get; set; }
}

[XmlRoot("FolderUpdate", Namespace = Constants.FolderHierarchy)]
public class FolderUpdateResponse
{
    [XmlElement("Status", Namespace = Constants.FolderHierarchy)]
    public int Status { get; set; } = 1;

    [XmlElement("SyncKey", Namespace = Constants.FolderHierarchy)]
    public string? SyncKey { get; set; }
    public bool ShouldSerializeSyncKey() => SyncKey is not null;
}

[XmlRoot("FolderDelete", Namespace = Constants.FolderHierarchy)]
public class FolderDelete
{
    [XmlElement("SyncKey", Namespace = Constants.FolderHierarchy)]
    public string? SyncKey { get; set; }

    [XmlElement("ServerId", Namespace = Constants.FolderHierarchy)]
    public string? ServerId { get; set; }
}

[XmlRoot("FolderDelete", Namespace = Constants.FolderHierarchy)]
public class FolderDeleteResponse
{
    [XmlElement("Status", Namespace = Constants.FolderHierarchy)]
    public int Status { get; set; } = 1;

    [XmlElement("SyncKey", Namespace = Constants.FolderHierarchy)]
    public string? SyncKey { get; set; }
    public bool ShouldSerializeSyncKey() => SyncKey is not null;
}
