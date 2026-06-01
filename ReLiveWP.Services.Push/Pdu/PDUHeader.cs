namespace ReLiveWP.Services.Push.Pdu;

public abstract class PDUHeader
{
    public abstract PDUHeaderType HeaderType { get; }
    public abstract int Length { get; }

    public abstract bool Read(BinaryReader reader, out PDUHeaderType nextType);
    public abstract bool Write(BinaryWriter writer, PDUHeaderType nextType);
}
