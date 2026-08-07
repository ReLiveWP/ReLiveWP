using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string ItemOperations = "ItemOperations";
}

// mailbox-store Fetch (PIM and FileReference/attachment), EmptyFolderContents, and Move
// are modelled; document-library fetch and Schema are not
[XmlRoot("ItemOperations", Namespace = Constants.ItemOperations)]
public class ItemOperations
{
    [XmlElement("Fetch", Namespace = Constants.ItemOperations)]
    public List<ItemOperationsFetch> Fetch { get; set; } = [];

    [XmlElement("EmptyFolderContents", Namespace = Constants.ItemOperations)]
    public List<ItemOperationsEmptyFolder> EmptyFolderContents { get; set; } = [];

    [XmlElement("Move", Namespace = Constants.ItemOperations)]
    public List<ItemOperationsMove> Move { get; set; } = [];
}

public class ItemOperationsEmptyFolder
{
    [XmlElement("Store", Namespace = Constants.ItemOperations)]
    public string? Store { get; set; }

    [XmlElement("CollectionId", Namespace = Constants.AirSync)]
    public string? CollectionId { get; set; }

    [XmlElement("Options", Namespace = Constants.ItemOperations)]
    public ItemOperationsEmptyFolderOptions? Options { get; set; }
}

public class ItemOperationsEmptyFolderOptions
{
    [XmlElement("DeleteSubFolders", Namespace = Constants.ItemOperations)]
    public int? DeleteSubFolders { get; set; }
}

public class ItemOperationsFetch
{
    [XmlElement("Store", Namespace = Constants.ItemOperations)]
    public string? Store { get; set; }

    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string? ServerId { get; set; }

    [XmlElement("CollectionId", Namespace = Constants.AirSync)]
    public string? CollectionId { get; set; }

    [XmlElement("LongId", Namespace = "Search")]
    public string? LongId { get; set; }

    // set for an attachment fetch; mutually exclusive with ServerId
    [XmlElement("FileReference", Namespace = Constants.AirSyncBase)]
    public string? FileReference { get; set; }

    [XmlElement("Options", Namespace = Constants.ItemOperations)]
    public ItemOperationsFetchOptions? Options { get; set; }
}

public class ItemOperationsFetchOptions
{
    [XmlElement("MIMESupport", Namespace = Constants.AirSync)]
    public int? MIMESupport { get; set; }

    // byte range for an attachment (FileReference) fetch only; no meaning for a PIM (ServerId) fetch
    [XmlElement("Range", Namespace = Constants.ItemOperations)]
    public string? Range { get; set; }

    [XmlElement("BodyPreference", Namespace = Constants.AirSyncBase)]
    public List<BodyPreference> BodyPreference { get; set; } = [];

    // parsed for parity with Sync's SyncOptions.BodyPartPreference, but unused
    [XmlElement("BodyPartPreference", Namespace = Constants.AirSyncBase)]
    public List<BodyPreference> BodyPartPreference { get; set; } = [];
}

// moves an entire conversation (ConversationId) to DstFldId; distinct from the
// single-item MoveItems command (Models/Move.cs)
public class ItemOperationsMove
{
    // serialised as base64 text; ASWBXML re-encodes as opaque binary on the wire, matching
    // the equivalent Email2:ConversationId field's convention
    [XmlElement("ConversationId", Namespace = Constants.ItemOperations)]
    public byte[]? ConversationId { get; set; }

    [XmlElement("DstFldId", Namespace = Constants.ItemOperations)]
    public string? DstFldId { get; set; }

    [XmlElement("Options", Namespace = Constants.ItemOperations)]
    public ItemOperationsMoveOptions? Options { get; set; }
}

public class ItemOperationsMoveOptions
{
    // empty-tag presence flag, MUST be included for a move operation; its absence produces
    // Status 155 (MoveAlways-absent), validated before calling the backend
    [XmlElement("MoveAlways", Namespace = Constants.ItemOperations)]
    public ItemOperationsMoveAlways? MoveAlways { get; set; }
}

public class ItemOperationsMoveAlways { }

[XmlRoot("ItemOperations", Namespace = Constants.ItemOperations)]
public class ItemOperationsResponse
{
    [XmlElement("Status", Namespace = Constants.ItemOperations)]
    public int Status { get; set; } = 1;

    [XmlElement("Response", Namespace = Constants.ItemOperations)]
    public ItemOperationsResponseBody? Response { get; set; }
}

public class ItemOperationsResponseBody
{
    [XmlElement("Fetch", Namespace = Constants.ItemOperations)]
    public List<ItemOperationsFetchResponse> Fetch { get; set; } = [];

    [XmlElement("EmptyFolderContents", Namespace = Constants.ItemOperations)]
    public List<ItemOperationsEmptyFolderResponse> EmptyFolderContents { get; set; } = [];

    [XmlElement("Move", Namespace = Constants.ItemOperations)]
    public List<ItemOperationsMoveResponse> Move { get; set; } = [];
}

public class ItemOperationsMoveResponse
{
    [XmlElement("ConversationId", Namespace = Constants.ItemOperations)]
    public byte[]? ConversationId { get; set; }

    // spec status 156 (destination must be IPF.Note) is unreachable here: conversations
    // are email-only by construction, so it never legitimately fires
    [XmlElement("Status", Namespace = Constants.ItemOperations)]
    public int Status { get; set; } = 1;
}

// no Store here: it's a request-only field. Strict clients (WP7/WP8/W8.1) reject a
// response that includes it; only lenient parsers (e.g. iOS Mail) silently tolerate it
public class ItemOperationsEmptyFolderResponse
{
    [XmlElement("Status", Namespace = Constants.ItemOperations)]
    public int Status { get; set; } = 1;

    [XmlElement("CollectionId", Namespace = Constants.AirSync)]
    public string? CollectionId { get; set; }
}

public class ItemOperationsFetchResponse
{
    [XmlElement("Status", Namespace = Constants.ItemOperations)]
    public int Status { get; set; } = 1;

    [XmlElement("CollectionId", Namespace = Constants.AirSync)]
    public string? CollectionId { get; set; }

    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string? ServerId { get; set; }

    [XmlElement("Class", Namespace = Constants.AirSync)]
    public string? Class { get; set; }

    // echoed back for an attachment (FileReference) fetch; absent for a PIM fetch
    [XmlElement("FileReference", Namespace = Constants.AirSyncBase)]
    public string? FileReference { get; set; }

    [XmlElement("Properties", Namespace = Constants.ItemOperations)]
    public ItemOperationsProperties? Properties { get; set; }
}

public class ItemOperationsProperties
{
    // attachment (FileReference) fetch fields; the response Range is authoritative and
    // may differ from the request's Range if the server couldn't fulfil it exactly
    [XmlElement("Range", Namespace = Constants.ItemOperations)]
    public string? Range { get; set; }

    [XmlElement("Total", Namespace = Constants.ItemOperations)]
    public int? Total { get; set; }
    public bool ShouldSerializeTotal() => Total.HasValue;

    // placed immediately before Data, matching the real device wire order
    [XmlElement("ContentType", Namespace = Constants.AirSyncBase)]
    public string? ContentType { get; set; }

    // base64-encoded inline content; mutually exclusive with Part
    [XmlElement("Data", Namespace = Constants.ItemOperations)]
    public string? Data { get; set; }

    // index (1-based; part 0 is always the WBXML control document itself) into the
    // MultiPartResponse's Parts array holding this content's raw bytes. Mutually exclusive with Data
    [XmlElement("Part", Namespace = Constants.ItemOperations)]
    public int? Part { get; set; }
    public bool ShouldSerializePart() => Part.HasValue;

    // PIM (ServerId) fetch: raw ApplicationData elements passed straight through
    [XmlAnyElement]
    public List<XmlElement> Elements { get; set; } = [];
}
