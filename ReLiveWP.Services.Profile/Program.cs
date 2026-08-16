using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;
using ReLiveWP.Services.Profile.Services;
using SoapCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddSoapCore();
builder.Services.AddControllers();

// we're not using the standard LiveID auth middleware here because tokens come in via SOAP headers
builder.Services.AddGrpcClient<Authentication.AuthenticationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));
builder.Services.AddGrpcClient<MailboxStore.MailboxStoreClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Mailbox"]!));
builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IProfileService, ProfileService>();

var app = builder.Build();

app.UseRouting();

#pragma warning disable ASP0014 // Suggest using top level route registrations
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapDefaultHealthCheckEndpoints();
    endpoints.UseSoapEndpoint<IProfileService>(o =>
    {
        o.Path = "/profile/profile.asmx";
        o.SoapSerializer = SoapSerializer.XmlSerializer;
    });
});
#pragma warning restore ASP0014 // Suggest using top level route registrations

app.Run();
