namespace ReLiveWP.Services.Login.Models;

public class InlineLoginOptions
{
    public const string SectionName = "InlineLogin";

    public DaTokenForm DaTokenForm { get; set; } = DaTokenForm.Envelope;
    public PuidFormat PuidFormat { get; set; } = PuidFormat.Hex16;

    public HashSet<string> Suppress { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Extra { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public enum DaTokenForm
{
    // the whole <EncryptedData Id="BinaryDAToken0"> element. verified correct for Phone8.1
    Envelope,
    // just the base64 blob out of CipherValue
    CipherValue,
    // ct=...&da=<url-encoded envelope>, the shape GetUserKeyData and DeviceAssociate receive
    WireForm
}

public enum PuidFormat
{
    Hex16,
    Decimal
}
