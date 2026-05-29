using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string AirSync = "AirSync";
    public const string AirSyncBase = "AirSyncBase";
}

// FilterType: used in Sync and GetItemEstimate Options
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
    IncompleteTasksOnly = 8,  // Tasks class only
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

// BodyPreference/Body Type: used in AirSyncBase BodyPreference and Body elements
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
    // Top-level status, only present for command-level errors (e.g. 13, 14).
    // Omit (null) on success — per-collection Status values are used instead.
    [XmlElement("Status", Namespace = Constants.AirSync)]
    public int? Status { get; set; }
    public bool ShouldSerializeStatus() => Status.HasValue;

    [XmlElement("Collections", Namespace = Constants.AirSync)]
    public SyncCollections? Collections { get; set; }

    // Present in partial sync requests/responses; empty element, null = absent
    [XmlIgnore]
    public bool Partial { get; set; }

    [XmlElement("Partial", Namespace = Constants.AirSync)]
    public string? PartialXml
    {
        get => Partial ? string.Empty : null;
        set => Partial = value != null;
    }

    // Client sends Wait (1–59 minutes) to hold the response open for changes
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
    // Only sent in protocol 12.1 requests; implied by CollectionId in 14.0+
    [XmlElement("Class", Namespace = Constants.AirSync)]
    public string? Class { get; set; }

    [XmlElement("SyncKey", Namespace = Constants.AirSync)]
    public string SyncKey { get; set; } = "0";

    [XmlElement("CollectionId", Namespace = Constants.AirSync)]
    public string CollectionId { get; set; } = string.Empty;

    // ── Request-only fields (suppressed in responses) ─────────────────────────

    // Request: move deleted items to Deleted Items folder instead of hard-deleting; 0|1
    [XmlElement("DeletesAsMoves", Namespace = Constants.AirSync)]
    public int? DeletesAsMoves { get; set; }
    public bool ShouldSerializeDeletesAsMoves() => DeletesAsMoves.HasValue;

    // Request: whether to return server-side changes; 0|1
    [XmlElement("GetChanges", Namespace = Constants.AirSync)]
    public int? GetChanges { get; set; }
    public bool ShouldSerializeGetChanges() => GetChanges.HasValue;

    // Request: max items to sync per response (1–512, default 100)
    [XmlElement("WindowSize", Namespace = Constants.AirSync)]
    public int? WindowSize { get; set; }
    public bool ShouldSerializeWindowSize() => WindowSize.HasValue;

    // Request: group messages by conversation; 0|1
    [XmlElement("ConversationMode", Namespace = Constants.AirSync)]
    public int? ConversationMode { get; set; }
    public bool ShouldSerializeConversationMode() => ConversationMode.HasValue;

    // Request: properties the client can store (tells server not to ghost them)
    [XmlElement("Supported", Namespace = Constants.AirSync)]
    public SyncSupported? Supported { get; set; }

    [XmlElement("Options", Namespace = Constants.AirSync)]
    public SyncOptions? Options { get; set; }

    // ── Response fields ───────────────────────────────────────────────────────

    // Status must precede Commands/Responses per MS-ASCMD §2.2.3.29.2
    [XmlElement("Status", Namespace = Constants.AirSync)]
    public int Status { get; set; }

    // Empty element; present when server has more items to send
    [XmlIgnore]
    public bool MoreAvailable { get; set; }

    [XmlElement("MoreAvailable", Namespace = Constants.AirSync)]
    public string? MoreAvailableXml
    {
        get => MoreAvailable ? string.Empty : null;
        set => MoreAvailable = value != null;
    }

    // Client→server commands in requests; server→client commands in responses
    [XmlElement("Commands", Namespace = Constants.AirSync)]
    public SyncCommands? Commands { get; set; }

    // Response: per-item status for client-originated Add/Change/Fetch
    [XmlElement("Responses", Namespace = Constants.AirSync)]
    public SyncResponses? Responses { get; set; }
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

    // MIMETruncation: 0=No truncation, 1–7=truncate to 512/1024/2048/5120/10240/20480/51200 bytes, 8=truncate all
    [XmlElement("MIMETruncation", Namespace = Constants.AirSync)]
    public int? MIMETruncation { get; set; }

    // Sent in Options; may appear multiple times for different body types
    [XmlElement("BodyPreference", Namespace = Constants.AirSyncBase)]
    public List<BodyPreference> BodyPreference { get; set; } = [];

    // BodyPartPreference: used for conversation-mode fetching of partial message bodies
    [XmlElement("BodyPartPreference", Namespace = Constants.AirSyncBase)]
    public List<BodyPreference> BodyPartPreference { get; set; } = [];

    // Client annotation subscription — which Live annotation names to include per item
    [XmlElement("Annotations", Namespace = Constants.WindowsLive)]
    public Annotations? Annotations { get; set; }
}

// Used in both Sync Options (request) and other commands that reference AirSyncBase
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

    // Return full body or nothing at all; 0|1
    [XmlElement("AllOrNone", Namespace = Constants.AirSyncBase)]
    public int? AllOrNone { get; set; }

    // Max length of preview text to include (≤255)
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

    // Server-only: soft-deleted item (moved to Deleted Items, not permanently removed)
    [XmlElement("SoftDelete", Namespace = Constants.AirSync)]
    public List<SyncItemRef> SoftDelete { get; set; } = [];
}

public class SyncAdd
{
    // Client→server: new item to create; ServerId is null (assigned by server)
    // Server→client: new item pushed to client; ClientId is null
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

// Shared by Delete, Fetch, and SoftDelete (all are just a ServerId reference)
public class SyncItemRef
{
    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string ServerId { get; set; } = string.Empty;
}

public class SyncResponses
{
    // Status per Add the client sent
    [XmlElement("Add", Namespace = Constants.AirSync)]
    public List<SyncResponseAdd> Add { get; set; } = [];

    // Status per Change the client sent
    [XmlElement("Change", Namespace = Constants.AirSync)]
    public List<SyncResponseChange> Change { get; set; } = [];

    // Response to a client Fetch: item data + status
    [XmlElement("Fetch", Namespace = Constants.AirSync)]
    public List<SyncResponseFetch> Fetch { get; set; } = [];
}

public class SyncResponseAdd
{
    [XmlElement("ClientId", Namespace = Constants.AirSync)]
    public string ClientId { get; set; } = string.Empty;

    // Present on success; null if server rejected the add
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

public class SyncResponseFetch
{
    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string ServerId { get; set; } = string.Empty;

    [XmlElement("Status", Namespace = Constants.AirSync)]
    public int Status { get; set; }

    [XmlElement("ApplicationData", Namespace = Constants.AirSync)]
    public ApplicationData? ApplicationData { get; set; }
}

// Generic container for item properties. Actual content is from the collection's
// class namespace (Email, Calendar, Contacts, Tasks) — use XmlAnyElement to
// round-trip the elements until per-class DTOs are available.
public class ApplicationData
{
    [XmlAnyElement]
    public List<XmlElement> Elements { get; set; } = [];
}

// Tells the server which properties the client can store.
// Content is class-specific property stubs from the collection's namespace.
public class SyncSupported
{
    [XmlAnyElement]
    public List<XmlElement> Elements { get; set; } = [];
}
