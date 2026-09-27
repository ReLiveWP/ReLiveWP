namespace ReLiveWP.Services.Login.Models.RST2;

public class RST2FailureModel(uint hr)
{
    public string RequestStatus { get; set; } = $"0x{hr:X8}";
}
