using ReLiveWP.Identity;
using ReLiveWP.Services.AddressBook.Services;
using ReLiveWP.Services.Grpc;
using SoapCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

// Add services to the container.

builder.Services.AddSoapCore();

builder.Services.AddControllers();

builder.Services.AddLiveIDAuthentication(o =>
{
    o.ConnectedServicesGrpcConfiguration = c => c.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!);
    o.LiveIDConfiguration = c => c.ValidServiceTargets = AddressBookService.TicketTargets;
});

// abservice.asmx gets its ticket in the ABAuthHeader SOAP header, which the LiveID handler never sees
builder.Services.AddSoapTicketVerification(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));
builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));

builder.Services.AddTransient<IAddressBookService, AddressBookService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
  
#pragma warning disable ASP0014 // Suggest using top level route registrations
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.UseSoapEndpoint<IAddressBookService>(o =>
    {
        o.Path = "/abservice/abservice.asmx";
        o.SoapSerializer = SoapSerializer.XmlSerializer;
    });
});
#pragma warning restore ASP0014 // Suggest using top level route registrations

app.MapDefaultEndpoints();

app.Run();
