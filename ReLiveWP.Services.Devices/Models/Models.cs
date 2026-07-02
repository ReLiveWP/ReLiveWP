namespace ReLiveWP.Services.Devices.Models;
public record ConnectedDeviceModel(
    string Id,
    string FriendlyName,
    string? Manufacturer, 
    string? Model, 
    string? Operator,
    string? PhoneNumber, 
    string? IMEI,
    string OSVersion, 
    string Locale,
    string Timezone,
    int ColourTheme,
    string? AccentColour);

public record ConnectedDeviceExtendedModel(
    string Id,
    string Manufacturer,
    string Model,
    string Operator,
    string? PhoneNumber,
    string OSVersion,
    string FriendlyName,
    int ColourTheme,
    string? AccentColour,
    string Locale,
    string Timezone,
    int? BatteryLevel,
    long? StorageRemaining,
    bool? IsPinLocked,
    bool? IsSimLocked,
    int? WorkingSet,
    DateTimeOffset? LastSeen,
    double? LastSeenLatitude,
    double? LastSeenLongitude,
    string? IMEI);

public record CommandStatusModel(
    uint RequestId,
    string Action,
    uint Result,
    bool Final,
    string? Data,
    DateTimeOffset? Reported,
    double? Latitude,
    double? Longitude,
    double? Accuracy);
