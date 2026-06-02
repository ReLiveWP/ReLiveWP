namespace ReLiveWP.Services.Push.Pdu;

public enum PDUCommand : byte
{
    Ack = 0x00,
    Connect = 0x01,
    Reconnect = 0x02,
    Disconnect = 0x03,
    KeepAlive = 0x04,
    Data = 0x11,
    ConnectResponse = 0x81,
    ReconnectResponse = 0x82,
    DisconnectResponse = 0x83,
    KeepAliveResponse = 0x84,
    Error = 0xFF
}
