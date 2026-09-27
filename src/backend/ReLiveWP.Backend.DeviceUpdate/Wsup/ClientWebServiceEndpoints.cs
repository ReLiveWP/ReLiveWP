using Microsoft.Extensions.Options;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public static class ClientWebServiceEndpoints
{
    private const string ClientService = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/";
    private const string SimpleAuthService = "http://www.microsoft.com/SoftwareDistribution/Server/SimpleAuthWebService/";
    private const string RegulationService = "http://www.microsoft.com/SoftwareDistribution/Server/UpdateRegulationWebService/";

    public static void MapClientWebService(this WebApplication app)
    {
        app.UseWhen(context => context.Request.Headers.ContainsKey("SOAPAction"),
            branch => branch.UseMiddleware<DeviceTrafficLogger>());

        app.MapMethods("/WM/MicrosoftUpdate/redir/duredir.cab", ["HEAD", "GET"], (IWebHostEnvironment environment) =>
            CabFile(environment, "duredir.cab"));

        app.MapMethods("/WM/MicrosoftUpdate/selfupdate/duident.cab", ["HEAD", "GET"], (IWebHostEnvironment environment) =>
            CabFile(environment, "duident.cab"));

        app.MapMethods("/Packages/{file}", ["HEAD", "GET"], (string file, IOptions<PackageOptions> packages) =>
        {
            var path = Path.Combine(packages.Value.ResolvedPath, Path.GetFileName(file));
            return File.Exists(path)
                ? Results.File(path, "application/vnd.ms-cab-compressed")
                : Results.NotFound();
        });

        app.MapPost("/v6/{webService=ClientWebService}/{filename=client.asmx}", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(HttpContext context, IWebHostEnvironment environment, UpdateService updates)
    {
        var action = context.Request.Headers["SOAPAction"].FirstOrDefault()?.Trim('"');

        switch (action)
        {
            case ClientService + "GetConfig":
                return CannedXml(environment, "client_config.xml");

            case SimpleAuthService + "GetAuthorizationCookie":
                return CannedXml(environment, "auth_cookie.xml");

            case RegulationService + "GetUpdateDownloadInformation":
                return CannedXml(environment, "update_regulation.xml");

            case ClientService + "GetCookie":
                return Xml(WsupXml.BuildResponse("GetCookieResponse",
                    WsusCookie.IssueCookie().ToResponse(WsupXml.Service, "GetCookieResult")));

            case ClientService + "SyncUpdates":
                {
                    var result = await updates.SyncUpdatesAsync(await ReadBodyAsync(context));
                    context.Items[DeviceTrafficLogger.SummaryKey] = result;
                    return Xml(result.Xml);
                }

            case ClientService + "GetExtendedUpdateInfo":
                {
                    var result = await updates.GetExtendedUpdateInfoAsync(await ReadBodyAsync(context));
                    context.Items[DeviceTrafficLogger.SummaryKey] = result;
                    return Xml(result.Xml);
                }
        }

        // TODO(wam): GetFileLocations, RegisterComputer, RefreshCache, ReportEventBatch and
        // StartCategoryScan land here. The client handles SOAP faults (2.2.2.4) but not a 404.
        return Results.NotFound();
    }

    private static Task<string> ReadBodyAsync(HttpContext context) =>
        new StreamReader(context.Request.Body).ReadToEndAsync();

    private static IResult Xml(string body) => Results.Text(body, "text/xml; charset=utf-8");

    private static IResult CannedXml(IWebHostEnvironment environment, string name) =>
        Results.File(Path.Join(environment.WebRootPath, name), "text/xml; charset=utf-8");

    private static IResult CabFile(IWebHostEnvironment environment, string name) =>
        Results.File(Path.Join(environment.WebRootPath, name), "application/vnd.ms-cab-compressed");
}
