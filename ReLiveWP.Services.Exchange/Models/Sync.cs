using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string AirSync = "AirSync";
    public const string AirSyncBase = "AirSyncBase";
}

public enum FilterType
{
    NoFilter = 0,
    OneDayBack = 1,
    ThreeDaysBack = 2,
    OneWeekBack = 3,
    TwoWeeksBack = 4,
    OneMonthBack = 5,
    ThreeMonthsBack = 6,
    SixMonthsBack = 7,
    IncompleteTasksOnly = 8, // Tasks class only
}

public enum MIMESupport
{
    Never = 0,
    SMIMEOnly = 1,
    Always = 2,
}

public enum SyncConflict
{
    ClientWins = 0,
    ServerWins = 1,
}

public enum BodyType
{
    PlainText = 1,
    HTML = 2,
    RTF = 3,
    MIME = 4,
}

[XmlRoot("Sync", Namespace = Constants.AirSync)]
public class Sync
{
    // only present for command-level errors; null on success (per-collection Status is used instead)
    [XmlElement("Status", Namespace = Constants.AirSync)]
    public int? Status { get; set; }
    public bool ShouldSerializeStatus() => Status.HasValue;

    [XmlElement("Collections", Namespace = Constants.AirSync)]
    public SyncCollections? Collections { get; set; }

    [XmlIgnore]
    public bool Partial { get; set; }

    [XmlElement("Partial", Namespace = Constants.AirSync)]
    public string? PartialXml
    {
        get => Partial ? string.Empty : null;
        set => Partial = value != null;
    }

    // client sends Wait (1-59 minutes) to hold the response open for changes
    [XmlElement("Wait", Namespace = Constants.AirSync)]
    public int? Wait { get; set; }
    public bool ShouldSerializeWait() => Wait.HasValue;

    [XmlElement("HeartbeatInterval", Namespace = Constants.AirSync)]
    public int? HeartbeatInterval { get; set; }
    public bool ShouldSerializeHeartbeatInterval() => HeartbeatInterval.HasValue;

    [XmlElement("Limit", Namespace = Constants.AirSync)]
    public int? Limit { get; set; }
    public bool ShouldSerializeLimit() => Limit.HasValue;
}

public class SyncCollections
{
    [XmlElement("Collection", Namespace = Constants.AirSync)]
    public List<SyncCollection> Items { get; set; } = [];
}

public class SyncCollection
{
    [XmlElement("Class", Namespace = Constants.AirSync)]
    public string? Class { get; set; }

    [XmlElement("SyncKey", Namespace = Constants.AirSync)]
    public string SyncKey { get; set; } = "0";

    [XmlElement("CollectionId", Namespace = Constants.AirSync)]
    public string CollectionId { get; set; } = string.Empty;

    // move deleted items to Deleted Items instead of hard-deleting; 0|1
    [XmlElement("DeletesAsMoves", Namespace = Constants.AirSync)]
    public int? DeletesAsMoves { get; set; }
    public bool ShouldSerializeDeletesAsMoves() => DeletesAsMoves.HasValue;

    // null = absent; empty element = true; "0" = false; anything else = true.
    // absence is resolved against SyncKey by the caller (non-zero key => true)
    [XmlIgnore]
    public bool? GetChanges { get; set; }

    [XmlElement("GetChanges", Namespace = Constants.AirSync)]
    public string? GetChangesXml
    {
        get => GetChanges == true ? string.Empty : null;
        set => GetChanges = value is null ? null : value != "0";
    }

    // max items to sync per response (1-512, default 100)
    [XmlElement("WindowSize", Namespace = Constants.AirSync)]
    public int? WindowSize { get; set; }
    public bool ShouldSerializeWindowSize() => WindowSize.HasValue;

    [XmlElement("ConversationMode", Namespace = Constants.AirSync)]
    public int? ConversationMode { get; set; }
    public bool ShouldSerializeConversationMode() => ConversationMode.HasValue;

    // properties the client can store (tells server not to ghost them)
    [XmlElement("Supported", Namespace = Constants.AirSync)]
    public SyncSupported? Supported { get; set; }

    [XmlElement("Options", Namespace = Constants.AirSync)]
    public SyncOptions? Options { get; set; }

    // declaration order is serialization order: must precede Commands/Responses
    [XmlElement("Status", Namespace = Constants.AirSync)]
    public int Status { get; set; }

    [XmlIgnore]
    public bool MoreAvailable { get; set; }

    [XmlElement("MoreAvailable", Namespace = Constants.AirSync)]
    public string? MoreAvailableXml
    {
        get => MoreAvailable ? string.Empty : null;
        set => MoreAvailable = value != null;
    }

    // must stay declared above Commands: XmlSerializer emits members in declaration order,
    // and if Commands precedes Responses in the response, WP7 stops reading after Commands
    // and never records the client Add's ClientId->ServerId, surfacing "could not be
    // synchronised" even though the server returned Status 1
    [XmlElement("Responses", Namespace = Constants.AirSync)]
    public SyncResponses? Responses { get; set; }

    [XmlElement("Commands", Namespace = Constants.AirSync)]
    public SyncCommands? Commands { get; set; }
}

public class SyncOptions
{
    [XmlIgnore]
    public FilterType? FilterType { get; set; }

    [XmlElement("FilterType", Namespace = Constants.AirSync)]
    public int? FilterTypeInt
    {
        get => FilterType.HasValue ? (int)FilterType.Value : null;
        set => FilterType = value.HasValue ? (FilterType)value.Value : null;
    }

    [XmlIgnore]
    public SyncConflict? Conflict { get; set; }

    [XmlElement("Conflict", Namespace = Constants.AirSync)]
    public int? ConflictInt
    {
        get => Conflict.HasValue ? (int)Conflict.Value : null;
        set => Conflict = value.HasValue ? (SyncConflict)value.Value : null;
    }

    [XmlIgnore]
    public MIMESupport? MIMESupport { get; set; }

    [XmlElement("MIMESupport", Namespace = Constants.AirSync)]
    public int? MIMESupportInt
    {
        get => MIMESupport.HasValue ? (int)MIMESupport.Value : null;
        set => MIMESupport = value.HasValue ? (MIMESupport)value.Value : null;
    }

    // 0=no truncation, 1-7=truncate to 512/1024/2048/5120/10240/20480/51200 bytes, 8=truncate all
    [XmlElement("MIMETruncation", Namespace = Constants.AirSync)]
    public int? MIMETruncation { get; set; }

    [XmlElement("BodyPreference", Namespace = Constants.AirSyncBase)]
    public List<BodyPreference> BodyPreference { get; set; } = [];

    // used for conversation-mode fetching of partial message bodies
    [XmlElement("BodyPartPreference", Namespace = Constants.AirSyncBase)]
    public List<BodyPreference> BodyPartPreference { get; set; } = [];

    // which Live annotation names to include per item
    [XmlElement("Annotations", Namespace = Constants.WindowsLive)]
    public Annotations? Annotations { get; set; }
}

public class BodyPreference
{
    [XmlIgnore]
    public BodyType Type { get; set; }

    [XmlElement("Type", Namespace = Constants.AirSyncBase)]
    public int TypeInt
    {
        get => (int)Type;
        set => Type = (BodyType)value;
    }

    [XmlElement("TruncationSize", Namespace = Constants.AirSyncBase)]
    public int? TruncationSize { get; set; }

    // return full body or nothing at all; 0|1
    [XmlElement("AllOrNone", Namespace = Constants.AirSyncBase)]
    public int? AllOrNone { get; set; }

    // max length of preview text to include (<=255)
    [XmlElement("Preview", Namespace = Constants.AirSyncBase)]
    public int? Preview { get; set; }
}

public class SyncCommands
{
    [XmlElement("Add", Namespace = Constants.AirSync)]
    public List<SyncAdd> Add { get; set; } = [];

    [XmlElement("Change", Namespace = Constants.AirSync)]
    public List<SyncChange> Change { get; set; } = [];

    [XmlElement("Delete", Namespace = Constants.AirSync)]
    public List<SyncItemRef> Delete { get; set; } = [];

    [XmlElement("Fetch", Namespace = Constants.AirSync)]
    public List<SyncItemRef> Fetch { get; set; } = [];

    // server-only: soft-deleted item (moved to Deleted Items, not permanently removed)
    [XmlElement("SoftDelete", Namespace = Constants.AirSync)]
    public List<SyncItemRef> SoftDelete { get; set; } = [];
}

public class SyncAdd
{
    // client->server: new item to create, ServerId is null (assigned by server)
    // server->client: item pushed to client, ClientId is null
    [XmlElement("ClientId", Namespace = Constants.AirSync)]
    public string? ClientId { get; set; }

    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string? ServerId { get; set; }

    [XmlElement("ApplicationData", Namespace = Constants.AirSync)]
    public ApplicationData? ApplicationData { get; set; }
}

public class SyncChange
{
    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string ServerId { get; set; } = string.Empty;

    [XmlElement("ApplicationData", Namespace = Constants.AirSync)]
    public ApplicationData? ApplicationData { get; set; }
}

public class SyncItemRef
{
    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string ServerId { get; set; } = string.Empty;
}

public class SyncResponses
{
    [XmlElement("Add", Namespace = Constants.AirSync)]
    public List<SyncResponseAdd> Add { get; set; } = [];

    [XmlElement("Change", Namespace = Constants.AirSync)]
    public List<SyncResponseChange> Change { get; set; } = [];

    // MS-ASCMD 2.2.3.42.2: failed deletions get their own Responses/Delete entry, not a Change
    [XmlElement("Delete", Namespace = Constants.AirSync)]
    public List<SyncResponseDelete> Delete { get; set; } = [];

    [XmlElement("Fetch", Namespace = Constants.AirSync)]
    public List<SyncResponseFetch> Fetch { get; set; } = [];
}

public class SyncResponseAdd
{
    [XmlElement("ClientId", Namespace = Constants.AirSync)]
    public string ClientId { get; set; } = string.Empty;

    // present on success; null if server rejected the add
    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string? ServerId { get; set; }

    [XmlElement("Status", Namespace = Constants.AirSync)]
    public int Status { get; set; }
}

public class SyncResponseChange
{
    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string ServerId { get; set; } = string.Empty;

    [XmlElement("Status", Namespace = Constants.AirSync)]
    public int Status { get; set; }
}

public class SyncResponseDelete
{
    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string ServerId { get; set; } = string.Empty;

    [XmlElement("Status", Namespace = Constants.AirSync)]
    public int Status { get; set; }
}

public class SyncResponseFetch
{
    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string ServerId { get; set; } = string.Empty;

    [XmlElement("Status", Namespace = Constants.AirSync)]
    public int Status { get; set; }

    [XmlElement("ApplicationData", Namespace = Constants.AirSync)]
    public ApplicationData? ApplicationData { get; set; }
}

public class ApplicationData
{
    [XmlAnyElement]
    public List<XmlElement> Elements { get; set; } = [];
}

public class SyncSupported
{
    [XmlAnyElement]
    public List<XmlElement> Elements { get; set; } = [];
}
