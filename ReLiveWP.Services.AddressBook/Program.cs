using ReLiveWP.Services.AddressBook.Services;
using SoapCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSoapCore();

builder.Services.AddControllers();

builder.Services.AddTransient<IAddressBookService, AddressBookService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseRouting();

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

app.Run();
