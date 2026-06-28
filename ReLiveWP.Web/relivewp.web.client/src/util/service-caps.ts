

export const enum ServiceCaps 
{
    none = 0,
    email = 0x1,
    contacts = 0x2,
    calendar = 0x4,
    messaging = 0x8,
    photoSync = 0x10,
    socialFeed = 0x20,
    socialPost = 0x40,
    socialCheckIn = 0x80,
    socialNotifications = 0x100,
    marketplaceStream = 0x200,
    marketplacePurchase = 0x400,
    fileStorage = 0x800,

    zune = 0x40000000,
    xbox = 0x80000000,

    all = email | 
          contacts | 
          calendar | 
          messaging | 
          photoSync | 
          socialFeed | 
          socialPost | 
          socialCheckIn | 
          socialNotifications | 
          marketplaceStream | 
          marketplacePurchase | 
          fileStorage | 
          zune | 
          xbox
}

export const ServiceCapNames = {
    [ServiceCaps.email]: "Mail",
    [ServiceCaps.contacts]: "People",
    [ServiceCaps.calendar]: "Calendar",
    [ServiceCaps.messaging]: "Messenger",
    [ServiceCaps.photoSync]: "Photo Sync",
    [ServiceCaps.socialFeed]: "Social - Feeds",
    [ServiceCaps.socialPost]: "Social - Post",
    [ServiceCaps.socialCheckIn]: "Social - Check in",
    [ServiceCaps.socialNotifications]: "Social - Notifications",
    [ServiceCaps.marketplaceStream]: "Marketplace - Streaming",
    [ServiceCaps.marketplacePurchase]: "Marketplace - Purchases",
    [ServiceCaps.fileStorage]: "SkyDrive", // fuck you BSkyB <3
    [ServiceCaps.zune]: "Zune",
    [ServiceCaps.xbox]: "Xbox"
}