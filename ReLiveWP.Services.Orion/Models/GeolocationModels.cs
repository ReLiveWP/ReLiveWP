using System.Text.Json.Serialization;

namespace ReLiveWP.Services.Orion.Models;

#nullable enable
public class GeolocateRequest
{
    [JsonPropertyName("carrier")]
    public string? Carrier { get; set; }

    [JsonPropertyName("considerIp")]
    public bool? ConsiderIp { get; set; }

    [JsonPropertyName("homeMobileCountryCode")]
    public int? HomeMobileCountryCode { get; set; }

    [JsonPropertyName("homeMobileNetworkCode")]
    public int? HomeMobileNetworkCode { get; set; }

    [JsonPropertyName("radioType")]
    public string? RadioType { get; set; }

    [JsonPropertyName("bluetoothBeacons")]
    public List<BluetoothBeacon>? BluetoothBeacons { get; set; }

    [JsonPropertyName("cellTowers")]
    public List<CellTower>? CellTowers { get; set; }

    [JsonPropertyName("wifiAccessPoints")]
    public List<WifiAccessPoint>? WifiAccessPoints { get; set; }

    [JsonPropertyName("fallbacks")]
    public Fallbacks? Fallbacks { get; set; }
}

public class BluetoothBeacon
{
    [JsonPropertyName("macAddress")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [JsonPropertyName("signalStrength")]
    public int? SignalStrength { get; set; }
}

public class CellTower
{
    [JsonPropertyName("radioType")]
    public string? RadioType { get; set; }

    [JsonPropertyName("mobileCountryCode")]
    public int? MobileCountryCode { get; set; }

    [JsonPropertyName("mobileNetworkCode")]
    public int? MobileNetworkCode { get; set; }

    [JsonPropertyName("locationAreaCode")]
    public int? LocationAreaCode { get; set; }

    [JsonPropertyName("cellId")]
    public long? CellId { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [JsonPropertyName("psc")]
    public int? Psc { get; set; }

    [JsonPropertyName("signalStrength")]
    public int? SignalStrength { get; set; }

    [JsonPropertyName("timingAdvance")]
    public int? TimingAdvance { get; set; }
}

public class WifiAccessPoint
{
    [JsonPropertyName("macAddress")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("age")]
    public int? Age { get; set; }

    [JsonPropertyName("channel")]
    public int? Channel { get; set; }

    [JsonPropertyName("frequency")]
    public int? Frequency { get; set; }

    [JsonPropertyName("signalStrength")]
    public int? SignalStrength { get; set; }

    [JsonPropertyName("signalToNoiseRatio")]
    public int? SignalToNoiseRatio { get; set; }

    [JsonPropertyName("ssid")]
    public string? Ssid { get; set; }
}

public class Fallbacks
{
    [JsonPropertyName("lacf")]
    public bool? Lacf { get; set; }

    [JsonPropertyName("ipf")]
    public bool? Ipf { get; set; }
}

public class GeolocateResponse
{
    [JsonPropertyName("location")]
    public LatLng? Location { get; set; }

    [JsonPropertyName("accuracy")]
    public double? Accuracy { get; set; }

    [JsonPropertyName("fallback")]
    public string? Fallback { get; set; }
    [JsonPropertyName("error")]
    public GeolocateError? Error { get; set; }
}

public class LatLng
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}

public class GeolocateError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("errors")]
    public List<GeolocateErrorDetail>? Errors { get; set; }
}

public class GeolocateErrorDetail
{
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}