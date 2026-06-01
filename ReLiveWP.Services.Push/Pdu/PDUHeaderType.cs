namespace ReLiveWP.Services.Push.Pdu;

public enum PDUHeaderType : byte
{
    End = 0,
    SessionInfo = 0x1,
    Authenticate = 0x3,
    DeviceInfo = 0x4,
    Sequence = 0x5,
    SessionConfig = 0x6,
    Binding = 0x12,
    Timestamp = 0x14,
    Signature = 0x15,
    NetworkInfo = 0x16,
    KeepAlive = 0x17,
    TransportSessionConfig = 0x18,
    DataHint3 = 0x19,
    Error = 0xFE
}
