using System.Xml;

namespace ReLiveWP.Services.Exchange.Models;

// Parsed parameters from an ActiveSync HTTP request (MS-ASHTTP §2.2.1.1.1).
// Covers both text (?Cmd=…) and binary (base64 query value) URI formats.
public class ActiveSyncContext
{
    public string? ProtocolVersion { get; set; }   // e.g. "14.1", "14.0", "12.1"
    public string? User { get; set; }              // from ?User= or binary tag 8
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public EasCommand Command { get; set; }
    public uint? PolicyKey { get; set; }           // null when not supplied (pre-provisioning)

    // ── Authenticated identity ────────────────────────────────────────────────
    // Windows Live PUID (Passport Unique ID), used as the CID in Live annotations.
    // Null when the identity token does not carry a puid claim.
    public long? Cid { get; set; }


    // ── Command-specific URI parameters ──────────────────────────────────────
    public string? AttachmentName { get; set; }    // GetAttachment: attachment filename (tag 0)
    public string? CollectionId { get; set; }      // SmartForward/Reply: source folder
    public string? ItemId { get; set; }            // SmartForward/Reply: source message (tag 3)
    public string? LongId { get; set; }            // SmartForward/Reply: Search result ref (tag 4)
    public string? Occurrence { get; set; }        // SmartForward/Reply: recurring occurrence (tag 6)
    public bool SaveInSent { get; set; }           // binary tag 7 bit 0x01 / text "SaveInSent=T"
    public bool AcceptMultiPart { get; set; }      // binary tag 7 bit 0x02 (ItemOperations)

    // ── Decoded body ──────────────────────────────────────────────────────────
    // null for bodyless commands (GetAttachment) or non-WBXML content types.
    public XmlDocument? XmlDocument { get; set; }
    // ── Encoded body ──────────────────────────────────────────────────────────
    public string OutputXml { get; set; }
}
