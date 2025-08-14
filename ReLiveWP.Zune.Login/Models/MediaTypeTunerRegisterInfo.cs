namespace ReLiveWP.Zune.Commerce.Models;

public class MediaTypeTunerRegisterInfo : TunerRegisterInfo
{
    public TunerRegisterType RegisterType { get; set; }
    public bool Activated { get; set; }
    public bool Activable { get; set; }
}
