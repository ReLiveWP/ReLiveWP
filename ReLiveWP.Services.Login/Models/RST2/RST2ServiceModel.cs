namespace ReLiveWP.Services.Login.Models.RST2;

public class RST2ServiceModel
{
    public const string TimestampId = "Timestamp";
    public const string HeaderEncId = "EncHeader";
    public const string BodyId = "Body";

    public const string SignatureMarker = "<!-- ds:Signature -->";

    public string TimeZ { get; set; } = "";
    public string Time5MZ { get; set; } = "";

    public string SignNonce { get; set; } = "";
    public string EncNonce { get; set; } = "";

    public string DaTokenReference { get; set; } = "";

    public string HeaderCipherValue { get; set; } = "";
    public string BodyCipherValue { get; set; } = "";
}
