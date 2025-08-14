using System.Net.Http.Headers;
using System.Text;
using System.Xml;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapMethods("/WM/MicrosoftUpdate/redir/duredir.cab", ["HEAD", "GET"], (IWebHostEnvironment environment) =>
{
    return Results.File(Path.Join(environment.WebRootPath, "duredir.cab"), "application/vnd.ms-cab-compressed");
});


app.MapMethods("/WM/MicrosoftUpdate/selfupdate/duident.cab", ["HEAD", "GET"], (IWebHostEnvironment environment) =>
{
    return Results.File(Path.Join(environment.WebRootPath, "duident.cab"), "application/vnd.ms-cab-compressed");
});

var client = new HttpClient();

int x = 0;
int y = 0;
int z = 0;
app.MapPost("/v6/{webService=ClientWebService}/{filename=client.asmx}", async (HttpContext context, IWebHostEnvironment environment) =>
{
    var header = context.Request.Headers["SOAPAction"].First().Trim('"');
    Console.WriteLine(header);
    switch (header)
    {
        case "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetConfig":
            return Results.File(Path.Join(environment.WebRootPath, "client_config.xml"), "text/xml; charset=utf-8");
        case "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetCookie":
            return Results.File(Path.Join(environment.WebRootPath, "cookie.xml"), "text/xml; charset=utf-8");
        case "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncUpdates":
            {
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

                var num = ++x;

                await File.WriteAllTextAsync($"SyncUpdates\\{num}_SyncUpdatesRequest.xml", body);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://fe2.update.microsoft.com/v6/ClientWebService/client.asmx");
                request.Headers.Add("SOAPAction", "\"http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncUpdates\"");
                request.Content = new StringContent(body, new MediaTypeHeaderValue("text/xml"));

                var resp = await client.SendAsync(request);
                var body2 = await resp.Content.ReadAsStringAsync();

                await File.WriteAllTextAsync($"SyncUpdates\\{num}_SyncUpdatesResponse.xml", body2);

                return Results.Text(body2, "text/xml; charset=utf-8", Encoding.UTF8);
            }
        case "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtendedUpdateInfo":
            {
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

                var num = ++y;

                await File.WriteAllTextAsync($"GetExtendedUpdateInfo\\{num}_GetExtendedUpdateInfoRequest.xml", body);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://fe2.update.microsoft.com/v6/ClientWebService/client.asmx");
                request.Headers.Add("SOAPAction", "\"http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtendedUpdateInfo\"");
                request.Content = new StringContent(body, new MediaTypeHeaderValue("text/xml"));

                var resp = await client.SendAsync(request);
                var body2 = await resp.Content.ReadAsStringAsync();

                var xml = new XmlDocument();
                xml.LoadXml(body2);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
                nsmgr.AddNamespace("u", @"http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService");

                // //FileLocation/Url

                foreach (XmlNode element in xml.SelectNodes("//u:FileLocation/u:Url", nsmgr))
                {
                    _ = client.GetByteArrayAsync(element.InnerText)
                              .ContinueWith(async s =>
                              {
                                  await File.WriteAllBytesAsync(Path.Join("Packages", Path.GetFileName(element.InnerText)), s.Result);
                              });
                    Console.WriteLine(element.InnerText);
                }

                await File.WriteAllTextAsync($"GetExtendedUpdateInfo\\{num}_GetExtendedUpdateInfoResponse.xml", body2);

                return Results.Text(body2, "text/xml; charset=utf-8", Encoding.UTF8);
            }
        case "http://www.microsoft.com/SoftwareDistribution/Server/UpdateRegulationWebService/GetUpdateDownloadInformation":
            {
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

                var num = ++z;

                await File.WriteAllTextAsync($"UpdateRegulation\\{num}_GetUpdateDownloadInformationRequest.xml", body);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://fe2.update.microsoft.com/v6/UpdateRegulationService/UpdateRegulation.asmx");
                request.Headers.Add("SOAPAction", "\"http://www.microsoft.com/SoftwareDistribution/Server/UpdateRegulationWebService/GetUpdateDownloadInformation\"");
                request.Content = new StringContent(body, new MediaTypeHeaderValue("text/xml"));

                var resp = await client.SendAsync(request);
                var body2 = await resp.Content.ReadAsStringAsync();

                await File.WriteAllTextAsync($"UpdateRegulation\\{num}_GetUpdateDownloadInformationResponse.xml", body2);

                return Results.Text(body2, "text/xml; charset=utf-8", Encoding.UTF8);
            }
        case "http://www.microsoft.com/SoftwareDistribution/Server/SimpleAuthWebService/GetAuthorizationCookie":
            return Results.File(Path.Join(environment.WebRootPath, "auth_cookie.xml"), "text/xml; charset=utf-8");
    }

    return Results.NotFound();
});

app.Run();