using System.Text.Json;
using ReLiveWP.Identity;
using ReLiveWP.Services.Devices.Services;
using ReLiveWP.Services.Grpc.DeviceRegistration;
using ReLiveWP.Services.Grpc.FindMyPhone;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddLiveIDAuthentication((o) =>
{
    o.IdentityGrpcConfiguration = (c) => c.Address = new Uri(builder.Configuration["Endpoints:Identity"]!);
    o.ConnectedServicesGrpcConfiguration = (c) => c.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!);
    o.LiveIDConfiguration = (c) => c.ValidServiceTargets = ["http://Passport.NET/tb", "relivewp.net"];
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddGrpcClient<FindMyPhone.FindMyPhoneClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Skybox"]!));
builder.Services.AddGrpcClient<DeviceRegistration.DeviceRegistrationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:DeviceRegistration"]!));

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "*",
                      policy => policy.WithOrigins("*")
                          .WithHeaders("*")
                          .WithMethods("*"));
});

builder.Services.AddSingleton<ICarrierLookupService, CarrierLookupService>();
builder.Services.AddHostedService(sp => (CarrierLookupService)sp.GetRequiredService<ICarrierLookupService>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("*");
}

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
