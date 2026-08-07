using System.ServiceModel;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using ReLiveWP.Services.Docs.Dav;

namespace ReLiveWP.Services.Docs.Soap;

public static class CloudDocuments
{
    public const string Ns = "http://schemas.microsoft.com/clouddocuments";
}

public enum AccessLevel
{
    Read,
    ReadWrite,
    None,
}

public enum SharingLevel
{
    Public,
    Private,
    Shared,
    PublicUnlisted,
}

public class OperationRequest
{
    public string? ClientAppId { get; set; }
    public string? Market { get; set; }
    public string? SkyDocsServiceVersion { get; set; }
}

public class SharingLevelInfo
{
    public string? Description { get; set; }
    public SharingLevel Level { get; set; }
}

public class Library
{
    public AccessLevel AccessLevel { get; set; }
    public string? DavUrl { get; set; }
    public string? DisplayName { get; set; }
    public SharingLevelInfo? SharingLevelInfo { get; set; }
    public string? WebUrl { get; set; }
    public string? ResourceId { get; set; }
    public DateTime LastModifiedDate { get; set; }
}

public class Document
{
    public AccessLevel AccessLevel { get; set; }
    public string? DavUrl { get; set; }
    public string? DisplayName { get; set; }
    public bool IsNotebook { get; set; }
    public DateTime LastModifiedDate { get; set; }
    public string? Owner { get; set; }
    public string? ResourceId { get; set; }
    public SharingLevelInfo? SharingLevelInfo { get; set; }
    public string? ViewUrl { get; set; }
    public string? WacUrl { get; set; }
    public string? WebUrl { get; set; }
}

public class ProductInfo
{
    public string? HomePageUrl { get; set; }
    public bool IsSoapEnabled { get; set; }
    public bool IsSyncEnabled { get; set; }
    public string? LearnMoreUrl { get; set; }
    public string? ProductName { get; set; }
    public string? ServiceDisabledErrorMessage { get; set; }
    public string? ShortProductName { get; set; }
    public string? SignInMessage { get; set; }
    public string? SignUpMessage { get; set; }
    public string? SignUpUrl { get; set; }
}

public class SyncData : IXmlSerializable
{
    public List<MultiStatusEntry> Entries { get; init; } = [];

    public XmlSchema? GetSchema() => null;

    public void ReadXml(XmlReader reader) => reader.Skip();

    public void WriteXml(XmlWriter writer) => MultiStatusWriter.Write(writer, Entries);
}

[MessageContract(IsWrapped = true, WrapperName = "GetWebAccountInfoRequest", WrapperNamespace = CloudDocuments.Ns)]
public class GetWebAccountInfoRequest
{
    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 0)]
    public OperationRequest? BaseRequest { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 1)]
    public bool GetReadWriteLibrariesOnly { get; set; }
}

[MessageContract(IsWrapped = true, WrapperName = "GetWebAccountInfoResponse", WrapperNamespace = CloudDocuments.Ns)]
public class GetWebAccountInfoResponse
{
    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 0)]
    public string? AccountTitle { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 1)]
    [XmlArray("Libraries", Namespace = CloudDocuments.Ns)]
    [XmlArrayItem("Library", Namespace = CloudDocuments.Ns)]
    public Library[] Libraries { get; set; } = [];

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 2)]
    public string? NewLibraryUrl { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 3)]
    public ProductInfo? ProductInfo { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 4)]
    public string? SignedInUser { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 5)]
    public string? RootDavUrl { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 6)]
    [XmlArray("Documents", Namespace = CloudDocuments.Ns)]
    [XmlArrayItem("Document", Namespace = CloudDocuments.Ns)]
    public Document[] Documents { get; set; } = [];
}

[MessageContract(IsWrapped = true, WrapperName = "GetChangesSinceTokenRequest", WrapperNamespace = CloudDocuments.Ns)]
public class GetChangesSinceTokenRequest
{
    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 0)]
    public OperationRequest? BaseRequest { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 1)]
    public string? DavUrl { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 2)]
    public string? SyncToken { get; set; }
}

[MessageContract(IsWrapped = true, WrapperName = "GetChangesSinceTokenResponse", WrapperNamespace = CloudDocuments.Ns)]
public class GetChangesSinceTokenResponse
{
    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 0)]
    public int MinAmIAloneSyncInterval { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 1)]
    public int MinBackgroundSyncInterval { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 2)]
    public int MinRealtimeSyncInterval { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 3)]
    public SyncData? SyncData { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 4)]
    public string? SyncToken { get; set; }
}

[MessageContract(IsWrapped = true, WrapperName = "GetProductInfoRequest", WrapperNamespace = CloudDocuments.Ns)]
public class GetProductInfoRequest
{
    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 0)]
    public OperationRequest? BaseRequest { get; set; }
}

[MessageContract(IsWrapped = true, WrapperName = "GetProductInfoResponse", WrapperNamespace = CloudDocuments.Ns)]
public class GetProductInfoResponse
{
    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 0)]
    public string? HomePageUrl { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 1)]
    public bool IsSoapEnabled { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 2)]
    public bool IsSyncEnabled { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 3)]
    public string? LearnMoreUrl { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 4)]
    public string? ProductName { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 5)]
    public string? ServiceDisabledErrorMessage { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 6)]
    public string? ShortProductName { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 7)]
    public string? SignInMessage { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 8)]
    public string? SignUpMessage { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 9)]
    public string? SignUpUrl { get; set; }
}

[MessageContract(IsWrapped = true, WrapperName = "ResolveWebUrlRequest", WrapperNamespace = CloudDocuments.Ns)]
public class ResolveWebUrlRequest
{
    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 0)]
    public OperationRequest? BaseRequest { get; set; }

    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 1)]
    public string? WebUrl { get; set; }
}

[MessageContract(IsWrapped = true, WrapperName = "ResolveWebUrlResponse", WrapperNamespace = CloudDocuments.Ns)]
public class ResolveWebUrlResponse
{
    [MessageBodyMember(Namespace = CloudDocuments.Ns, Order = 0)]
    public string? DavUrl { get; set; }
}

public class ServerError
{
    public string? FailureDetail { get; set; }
    public string? MachineName { get; set; }
}
