using System.Buffers.Binary;

namespace ReLiveWP.Backend.Skybox.Commands;

public enum DeviceCommandAction : byte
{
    Ring = 1,
    Locate = 2,
    Lock = 3,
    Wipe = 4,
    DownloadApps = 5,
}

public enum DeviceCommandAckMode : byte
{
    None = 0,
    Optional = 1,
    Required = 2,
}

/// <summary>
/// Wire layout (all multi-byte integers big-endian):
/// <code>
///   [0]      format         = 0x01 (plaintext)
///   [1..2]   payloadLength  = 9 + CommandData.Length   (must be 9..256)
///   [3]      flags          = (AckMode &lt;&lt; 6) | (Action &amp; 0x3F)
///   [4..7]   RequestId      (the device drops duplicates)
///   [8..11]  Timestamp      (must be &gt;= the device's last processed command, replay guard)
///   [12..]   CommandData    (per-action; empty for Ring/Locate/Wipe)
/// </code>
/// </summary>
public sealed class DeviceCommand
{
    private const byte FormatPlaintext = 0x01;
    private const int HeaderLength = 9;        // flags(1) + requestId(4) + timestamp(4)
    private const int MaxPayloadLength = 256;  // the device rejects payloadLength > 0x100

    public required DeviceCommandAction Action { get; init; }

    public DeviceCommandAckMode AckMode { get; init; } = DeviceCommandAckMode.None;

    public byte[] CommandData { get; init; } = [];
    public uint RequestId { get; set; }
    public uint Timestamp { get; set; } = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public byte[] Serialize()
    {
        var payloadLength = HeaderLength + CommandData.Length;
        if (payloadLength > MaxPayloadLength)
            throw new InvalidOperationException(
                $"Command payload is {payloadLength} bytes; the device accepts at most {MaxPayloadLength}.");

        var buffer = new byte[3 + payloadLength];
        buffer[0] = FormatPlaintext;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(1), (ushort)payloadLength);
        buffer[3] = (byte)(((byte)AckMode << 6) | ((byte)Action & 0x3F));
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(4), RequestId);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(8), Timestamp);
        CommandData.CopyTo(buffer.AsSpan(12));
        return buffer;
    }


    public static DeviceCommand Ring() 
        => new() { Action = DeviceCommandAction.Ring };

    public static DeviceCommand Locate() 
        => new() { Action = DeviceCommandAction.Locate };

    public static DeviceCommand Wipe() 
        => new() { Action = DeviceCommandAction.Wipe };

    public static DeviceCommand Lock(bool lockDevice = true, ushort pin = 0, bool deviceHasPasscode = false)
    {
        var data = new byte[5];
        data[0] = (byte)(lockDevice ? 1 : 0);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(1), pin);
        // data[3]: secondary flag read into LockTask+0x4c; left 0 until its exact meaning is confirmed.
        data[4] = (byte)(deviceHasPasscode ? 1 : 0);
        return new() { Action = DeviceCommandAction.Lock, CommandData = data };
    }

    public static DeviceCommand DownloadApps(Guid productId, Guid instanceId)
    {
        var data = new byte[32];
        productId.ToByteArray().CopyTo(data, 0);
        instanceId.ToByteArray().CopyTo(data, 16);
        return new() { Action = DeviceCommandAction.DownloadApps, CommandData = data };
    }
}
