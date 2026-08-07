using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string Ping = "Ping";
}

// first request must carry both HeartbeatInterval and Folders; later requests may omit
// either (or send an empty body) and the server reuses the cached values
[XmlRoot("Ping", Namespace = Constants.Ping)]
public class Ping
{
    [XmlElement("HeartbeatInterval", Namespace = Constants.Ping)]
    public int? HeartbeatInterval { get; set; }
    public bool ShouldSerializeHeartbeatInterval() => HeartbeatInterval.HasValue;

    [XmlElement("Folders", Namespace = Constants.Ping)]
    public PingFolders? Folders { get; set; }
}

public class PingFolders
{
    [XmlElement("Folder", Namespace = Constants.Ping)]
    public List<PingFolder> Folder { get; set; } = [];
}

public class PingFolder
{
    [XmlElement("Id", Namespace = Constants.Ping)]
    public string Id { get; set; } = string.Empty;

    [XmlElement("Class", Namespace = Constants.Ping)]
    public string Class { get; set; } = string.Empty;
}

// response Folder elements are bare strings (the changed folder id), unlike request Folder
// which has Id/Class children
[XmlRoot("Ping", Namespace = Constants.Ping)]
public class PingResponse
{
    [XmlElement("Status", Namespace = Constants.Ping)]
    public int Status { get; set; }

    // returned with Status=5 (nearest allowed heartbeat)
    [XmlElement("HeartbeatInterval", Namespace = Constants.Ping)]
    public int? HeartbeatInterval { get; set; }
    public bool ShouldSerializeHeartbeatInterval() => HeartbeatInterval.HasValue;

    // returned with Status=2 (folders with changes)
    [XmlElement("Folders", Namespace = Constants.Ping)]
    public PingResponseFolders? Folders { get; set; }

    // returned with Status=6 (folder cap)
    [XmlElement("MaxFolders", Namespace = Constants.Ping)]
    public int? MaxFolders { get; set; }
    public bool ShouldSerializeMaxFolders() => MaxFolders.HasValue;
}

public class PingResponseFolders
{
    [XmlElement("Folder", Namespace = Constants.Ping)]
    public List<string> Folder { get; set; } = [];
}
