namespace ReLiveWP.ProductKeys;

public sealed record ProductKeyFields(bool IsUpgrade, int Serial, int Hash, long Signature)
{
    public const int SerialsPerChannel = 1_000_000;
    public const int ChannelCount = 1_000;

    public int ChannelId => Serial / SerialsPerChannel;
    public int Sequence => Serial % SerialsPerChannel;
    public int PackedSerial => Serial << 1 | (IsUpgrade ? 1 : 0);

    public static int ComposeSerial(int channelId, int sequence)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(channelId);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channelId, ChannelCount);
        ArgumentOutOfRangeException.ThrowIfNegative(sequence);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(sequence, SerialsPerChannel);

        return channelId * SerialsPerChannel + sequence;
    }
}
