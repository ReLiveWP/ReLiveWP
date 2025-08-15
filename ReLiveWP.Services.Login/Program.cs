using System.Text.Json;
using ReLiveWP.Identity;
using ReLiveWP.Services.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddLiveIDAuthentication((o) =>
{
    o.GrpcConfiguration = (c) => c.Address = new Uri(builder.Configuration["Endpoints:Identity"]!);
    o.LiveIDConfiguration = (c) => c.ValidServiceTargets = ["http://Passport.NET/tb"];
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddGrpcClient<Authentication.AuthenticationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));

var app = builder.Build();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
