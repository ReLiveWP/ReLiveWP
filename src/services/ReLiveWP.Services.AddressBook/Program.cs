using ReLiveWP.Identity;
using ReLiveWP.Services.AddressBook.Services;
using SoapCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

// Add services to the container.

builder.Services.AddSoapCore();

builder.Services.AddControllers();

builder.Services.AddLiveIDAuthentication(o =>
{
    o.ConnectedServicesGrpcConfiguration = c => c.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!);
    o.LiveIDConfiguration = c => c.ValidServiceTargets =
        ["http://Passport.NET/tb", "relivewp.net", "contacts.relivewp.net", "contacts.int.relivewp.net"];
});

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
