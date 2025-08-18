namespace ReLiveWP.Backend.Identity.Data;

[Flags]
public enum LiveConnectedServiceCapabilities : uint
{
    None = 0,
    Email = 0x1,
    Contacts = 0x2,
    Calendar = 0x4,
    Messaging = 0x8,
    PhotoSync = 0x10,
    SocialFeed = 0x20,
    SocialPost = 0x40,
    SocialCheckIn = 0x80,
    SocialNotifications = 0x100,
    MarketplaceStream = 0x200,
    MarketplacePurchase = 0x400,

    Zune = 0x40000000,
    Xbox = 0x80000000
}
