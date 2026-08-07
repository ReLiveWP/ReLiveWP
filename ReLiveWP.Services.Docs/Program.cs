using ReLiveWP.Identity;
using ReLiveWP.Identity.Grpc;
using ReLiveWP.Services.Docs.Soap;
using ReLiveWP.Services.Grpc.SkyDocs;
using SoapCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddSoapCore();
builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<AuthForwardingInterceptor>();

builder.Services.AddLiveIDAuthentication((o) =>
{
    o.ConnectedServicesGrpcConfiguration = c => c.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!);
    o.LiveIDConfiguration = (c) =>
    {
        c.ValidServiceTargets = [
            "http://Passport.NET/tb",
            "relivewp.net",
            "docs.relivewp.net",
            "docs.int.relivewp.net",
        ];

        c.CookieNameFallback = "RPSTAuth";
    };
});

builder.Services.AddGrpcClient<SkyDocs.SkyDocsClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:SkyDrive"]!))
    .AddInterceptor<AuthForwardingInterceptor>();

builder.Services.AddTransient<ISkyDocsService, SkyDocsService>();

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

#pragma warning disable ASP0014 // Suggest using top level route registrations
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.UseSoapEndpoint<ISkyDocsService>(o =>
    {
        o.Path = "/SkyDocsService.svc";
        o.SoapSerializer = SoapSerializer.XmlSerializer;
    }).RequireAuthorization();
});
#pragma warning restore ASP0014 // Suggest using top level route registrations

app.MapDefaultEndpoints();

app.Run();
