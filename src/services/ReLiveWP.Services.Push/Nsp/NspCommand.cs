namespace ReLiveWP.Services.Push.Nsp;

public enum NspCommand : byte
{
    CreateNotificationChannel = 0x01,
    Deregister = 0x02,
    Notification = 0x03,            // server -> device message delivery
    ServerCommand = 0x04,
    Configure = 0x05,
    EndpointFlush = 0x06,           // device: notification queue hit its high-water mark
    MessageConsumed = 0x07,         // device: app drained a delivered notification
    Enum = 0x09,

    CreateChannelResponse = 0x81,   // reply to CreateNotificationChannel
    DeregisterResponse = 0x82,      // reply to Deregister
    ConfigureResponse = 0x85,       // reply to Configure
    EndpointFlushResponse = 0x86,
    MessageConsumedResponse = 0x87,
    Error = 0xFE,
}
