namespace ReLiveWP.Services.Push.Nsp;

public enum NspTag : byte
{
    Kind = 0x04,
    ChannelId = 0x08,
    ChannelUri = 0x0C,
    Option = 0x2A,
    Identifier = 0x44,
    Name = 0x48,
    Version = 0x4C,
    Publisher = 0x50,
}

public static class NspTagExtensions
{
    public static bool IsString(this NspTag tag) => tag is
        NspTag.ChannelUri or NspTag.Identifier or NspTag.Name or NspTag.Version or NspTag.Publisher;
}
