using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Services.Orion.Models;

namespace ReLiveWP.Services.Orion.Controllers;

[ApiController]
[Route("[controller]Service/v21/pox/{action}")]
public class InferenceController(ILogger<InferenceController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    const string InferenceNs = "http://inference.location.live.com";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly XmlSerializer RequestSerializer
        = new XmlSerializer(typeof(GetLocationUsingFingerprintRequest));
    private static readonly XmlSerializer ResponseSerializer
        = new XmlSerializer(typeof(GetLocationUsingFingerprintResponse));
    private static readonly XmlSerializer TileRequestSerializer
        = new XmlSerializer(typeof(GetTileUsingPositionRequest));
    private static readonly XmlSerializer TileResponseSerializer
        = new XmlSerializer(typeof(GetTileUsingPositionResponse));

    [HttpPost]
    [ActionName("GetLocationUsingFingerprint")]
    public async Task<IActionResult> GetLocationUsingFingerprintAsync(CancellationToken ct = default)
    {
        // WP7's location framework gzips the request body separately, not the whole request, so we decompress manually
        using var body = new MemoryStream();
        await Request.Body.CopyToAsync(body, ct);
        body.Position = 0;

        GetLocationUsingFingerprintRequest request;
        using (var gzipStream = new GZipStream(body, CompressionMode.Decompress, true))
        using (var textReader = new StreamReader(gzipStream))
            request = (GetLocationUsingFingerprintRequest)RequestSerializer.Deserialize(textReader)!;

        var trackingId = request.RequestHeader?.TrackingId;
        var detections = request.BeaconFingerprint?.Detections;

        var geolocateRequest = BuildGeolocateRequest(detections);

        GeolocateResponse geolocate;
        try
        {
            geolocate = await ResolveAsync(geolocateRequest, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Geolocation inference upstream call failed (trackingId {TrackingId})", trackingId);
            return Xml(BuildFailureResponse(trackingId));
        }

        if (geolocate?.Location is null)
        {
            logger.LogWarning(
                "Geolocation inference returned no fix (trackingId {TrackingId}, error {Error})",
                trackingId, geolocate?.Error?.Message);
            return Xml(BuildFailureResponse(trackingId));
        }

        var response = new GetLocationUsingFingerprintResponse
        {
            GetLocationUsingFingerprintResult = new GetLocationUsingFingerprintResult
            {
                ResponseStatus = "Success",
                LocationResult = new LocationResult
                {
                    ResolverStatus = new ResolverStatus { Status = "Success", Source = "Internal" },
                    ResolvedPosition = new ResolvedPosition
                    {
                        Latitude = geolocate.Location.Lat,
                        Longitude = geolocate.Location.Lng,
                        Altitude = 0,
                    },
                    RadialUncertainty = (int)Math.Round(geolocate.Accuracy ?? 0),
                    TileResult = string.Empty,
                    TrackingId = trackingId,
                },
                ExtendedV21Result = new ExtendedV21Result
                {
                    CrowdSourcingLevel = "0",
                    ServerUtcTime = DateTime.UtcNow.ToString("o"),
                },
            },
        };

        return Xml(response);
    }

    private GeolocateRequest BuildGeolocateRequest(Detections detections)
    {
        var cellTowers = new List<CellTower>();
        var wifiAccessPoints = new List<WifiAccessPoint>();

        if (detections is not null)
        {
            foreach (var umts in detections.Umts7)
            {
                if (umts.Mcc > 0 && umts.Cid > 0)
                {
                    cellTowers.Add(new CellTower
                    {
                        RadioType = "wcdma",
                        MobileCountryCode = umts.Mcc,
                        MobileNetworkCode = umts.Mnc,
                        LocationAreaCode = umts.Lac,
                        CellId = umts.Cid,
                        Psc = umts.Psc,
                        SignalStrength = umts.Rscp,
                    });
                }

                foreach (var mrl in umts.Mrl7)
                {
                    // measurements only carry a usable global id sometimes, skip the ones
                    // that only have a PSC/ARFCN since Google needs at least mcc/mnc/cid to place them.
                    if (mrl.Mcc <= 0 || mrl.Cid <= 0) 
                        continue;

                    cellTowers.Add(new CellTower
                    {
                        RadioType = "wcdma",
                        MobileCountryCode = mrl.Mcc,
                        MobileNetworkCode = mrl.Mnc,
                        LocationAreaCode = mrl.Lac,
                        CellId = mrl.Cid,
                        Psc = mrl.Psc,
                        SignalStrength = mrl.Rscp,
                    });
                }

                foreach (var nmr in umts.Nmr7)
                {
                    if (nmr.Mcc <= 0 || nmr.Cid <= 0) continue;
                    cellTowers.Add(new CellTower
                    {
                        RadioType = "gsm",
                        MobileCountryCode = nmr.Mcc,
                        MobileNetworkCode = nmr.Mnc,
                        LocationAreaCode = nmr.Lac,
                        CellId = nmr.Cid,
                        SignalStrength = nmr.Rx,
                    });
                }
            }

            foreach (var wifi in detections.Wifi7)
            {
                if (string.IsNullOrEmpty(wifi.BssId)) continue;
                wifiAccessPoints.Add(new WifiAccessPoint
                {
                    MacAddress = wifi.BssId,
                    SignalStrength = wifi.Rssi,
                });
            }
        }

        var serving = cellTowers.Count > 0 ? cellTowers[0] : null;

        return new GeolocateRequest
        {
            // we cant base anything off the IP of our server
            ConsiderIp = false,
            RadioType = serving?.RadioType,
            HomeMobileCountryCode = serving?.MobileCountryCode,
            HomeMobileNetworkCode = serving?.MobileNetworkCode,
            CellTowers = cellTowers.Count > 0 ? cellTowers : null,
            WifiAccessPoints = wifiAccessPoints.Count > 0 ? wifiAccessPoints : null,
        };
    }

    private async Task<GeolocateResponse> ResolveAsync(GeolocateRequest geolocateRequest, CancellationToken ct)
    {
        var endpoint = configuration["Orion:InferenceEndpoint"];
        var apiKey = configuration["Orion:InferenceApiKey"];

        var url = endpoint;
        if (!string.IsNullOrEmpty(apiKey))
        {
            var separator = endpoint.Contains('?') ? '&' : '?';
            url = $"{endpoint}{separator}key={Uri.EscapeDataString(apiKey)}";
        }

        var client = httpClientFactory.CreateClient();
        using var httpResponse = await client.PostAsJsonAsync(url, geolocateRequest, JsonOptions, ct);

        return await httpResponse.Content.ReadFromJsonAsync<GeolocateResponse>(JsonOptions, ct);
    }

    private static GetLocationUsingFingerprintResponse BuildFailureResponse(string trackingId) => new()
    {
        GetLocationUsingFingerprintResult = new GetLocationUsingFingerprintResult
        {
            ResponseStatus = "Failed",
            LocationResult = new LocationResult
            {
                ResolverStatus = new ResolverStatus { Status = "Failed", Source = "None" },
                TileResult = string.Empty,
                TrackingId = trackingId,
            },
            ExtendedV21Result = new ExtendedV21Result
            {
                CrowdSourcingLevel = "0",
                ServerUtcTime = DateTime.UtcNow.ToString("o"),
            },
        },
    };

    [HttpPost]
    [ActionName("GetTileUsingPosition")]
    public async Task<IActionResult> GetTileUsingPositionAsync(CancellationToken ct = default)
    {
        // we currently dont generate offline lookup tiles yet, something to consider in future
        using var body = new MemoryStream();
        await Request.Body.CopyToAsync(body, ct);
        body.Position = 0;

        GetTileUsingPositionRequest request;
        using (var gzipStream = new GZipStream(body, CompressionMode.Decompress, true))
        using (var textReader = new StreamReader(gzipStream))
            request = (GetTileUsingPositionRequest)TileRequestSerializer.Deserialize(textReader)!;

        var trackingId = request.RequestHeader?.TrackingId;
        logger.LogInformation(
            "GetTileUsingPosition returning empty tile set (trackingId {TrackingId})", trackingId);

        var response = new GetTileUsingPositionResponse
        {
            GetTileUsingPositionResult = new GetTileUsingPositionResult
            {
                ResponseStatus = "Success",
                TrackingId = trackingId,
                TileSet = new TileSet { Count = 0, DataSuppressed = true },
                ExtendedV21Result = new ExtendedV21Result
                {
                    CrowdSourcingLevel = "0",
                    ServerUtcTime = DateTime.UtcNow.ToString("o"),
                },
            },
        };

        return Xml(response);
    }

    private IActionResult Xml(GetLocationUsingFingerprintResponse response)
        => Xml(response, ResponseSerializer);

    private IActionResult Xml(GetTileUsingPositionResponse response)
        => Xml(response, TileResponseSerializer);

    private IActionResult Xml(object response, XmlSerializer serializer)
    {
        var ns = new XmlSerializerNamespaces();
        ns.Add(string.Empty, InferenceNs);

        var stream = new MemoryStream();
        using (var xmlWriter = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new System.Text.UTF8Encoding(false) }))
            serializer.Serialize(xmlWriter, response, ns);

        stream.Seek(0, SeekOrigin.Begin);

        return File(stream, "text/xml");
    }
}
