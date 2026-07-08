using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Exchange.Models;

public static partial class Constants
{
    public const string ItemOperations = "ItemOperations";
}

// MS-ASCMD §2.2.1.10 — ItemOperations command. We implement the Mailbox-store Fetch case,
// used to retrieve a full (untruncated) item body on demand. Other operations
// (EmptyFolderContents, Move, document-library fetch, attachments) are not modelled yet.
[XmlRoot("ItemOperations", Namespace = Constants.ItemOperations)]
public class ItemOperations
{
    [XmlElement("Fetch", Namespace = Constants.ItemOperations)]
    public List<ItemOperationsFetch> Fetch { get; set; } = [];
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

    [XmlElement("Options", Namespace = Constants.ItemOperations)]
    public ItemOperationsFetchOptions? Options { get; set; }
}

public class ItemOperationsFetchOptions
{
    [XmlElement("MIMESupport", Namespace = Constants.AirSync)]
    public int? MIMESupport { get; set; }

    [XmlElement("BodyPreference", Namespace = Constants.AirSyncBase)]
    public List<BodyPreference> BodyPreference { get; set; } = [];
}

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
}

public class ItemOperationsFetchResponse
{
    [XmlElement("Status", Namespace = Constants.ItemOperations)]
    public int Status { get; set; } = 1;

    [XmlElement("Store", Namespace = Constants.ItemOperations)]
    public string? Store { get; set; }

    [XmlElement("CollectionId", Namespace = Constants.AirSync)]
    public string? CollectionId { get; set; }

    [XmlElement("ServerId", Namespace = Constants.AirSync)]
    public string? ServerId { get; set; }

    [XmlElement("Class", Namespace = Constants.AirSync)]
    public string? Class { get; set; }

    [XmlElement("Properties", Namespace = Constants.ItemOperations)]
    public ItemOperationsProperties? Properties { get; set; }
}

// Holds the class-specific property elements (Email/Calendar/Contacts), injected as raw XML.
public class ItemOperationsProperties
{
    [XmlAnyElement]
    public List<XmlElement> Elements { get; set; } = [];
}
