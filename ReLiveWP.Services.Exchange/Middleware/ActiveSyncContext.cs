using System.Xml;
using ReLiveWP.Services.Exchange.Models;

namespace ReLiveWP.Services.Exchange.Middleware;

public class ActiveSyncContext
{
    public string? ProtocolVersion { get; set; }
    public string? User { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public EasCommand Command { get; set; }
    public uint? PolicyKey { get; set; }           // null when not supplied (pre-provisioning)

    public string? AttachmentName { get; set; }    // GetAttachment: attachment filename
    public string? CollectionId { get; set; }      // SmartForward/Reply: source folder
    public string? ItemId { get; set; }            // SmartForward/Reply: source message
    public string? LongId { get; set; }            // SmartForward/Reply: Search result ref
    public string? Occurrence { get; set; }        // SmartForward/Reply: recurring occurrence
    public bool SaveInSent { get; set; }
    public bool AcceptMultiPart { get; set; }      // ItemOperations

    public XmlDocument? XmlDocument { get; set; }
    public string? OutputXml { get; set; }
}
