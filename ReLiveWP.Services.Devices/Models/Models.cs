namespace ReLiveWP.Services.Devices.Models;
public record ConnectedDeviceModel(
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