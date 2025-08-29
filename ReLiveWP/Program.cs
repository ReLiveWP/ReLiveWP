using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReLiveWP.Services;
using ReLiveWP;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<AspireProcessLauncher>();

var identity = builder.AddProject("ReLiveWP.Backend.Identity");
var registration = builder.AddProject("ReLiveWP.Backend.DeviceRegistration");
var deviceUpdate = builder.AddProject("ReLiveWP.Backend.DeviceUpdate");

builder.AddProject("ReLiveWP.Services.Activation")
    .DependsOn(registration);

builder.AddProject("ReLiveWP.Services.Login")
    .DependsOn(identity)
    .DependsOn(registration);

builder.AddProject("ReLiveWP.Services.Activity")
    .DependsOn(identity);

builder.AddProject("ReLiveWP.Services.Push");

builder.AddProject("ReLiveWP.Zune.Catalog");

builder.AddProject("ReLiveWP.Zune.Commerce")
    .DependsOn(identity);

builder.AddProject("ReLiveWP.Web.Server", "ReLiveWP.Web/ReLiveWP.Web.Server")
    .DependsOn(identity);

var app = builder.Build();
app.Run();