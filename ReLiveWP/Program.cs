using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReLiveWP.Services;
using ReLiveWP;

var builder = Host.CreateApplicationBuilder(args);

if (Environment.GetEnvironmentVariable("DEBUG_SESSION_PORT") != null)
{
    builder.Services.AddHostedService<AspireProcessLauncher>();
}
else
{
    builder.Services.AddHostedService<StandardProcessLauncher>();
}

var identity = builder.AddProject("ReLiveWP.Backend.Identity");
var connectedServices = builder.AddProject("ReLiveWP.Backend.ConnectedServices")
    .DependsOn(identity);
var registration = builder.AddProject("ReLiveWP.Backend.DeviceRegistration");
var deviceUpdate = builder.AddProject("ReLiveWP.Backend.DeviceUpdate");

builder.AddProject("ReLiveWP.Services.Activation")
    .DependsOn(registration);

builder.AddProject("ReLiveWP.Services.Login")
    .DependsOn(identity)
    .DependsOn(connectedServices)
    .DependsOn(registration);

builder.AddProject("ReLiveWP.Services.Activity")
    .DependsOn(identity)
    .DependsOn(connectedServices);

builder.AddProject("ReLiveWP.Services.Push");

builder.AddProject("ReLiveWP.Services.AddressBook")
    .DependsOn(identity);

builder.AddProject("ReLiveWP.Services.Exchange")
    .DependsOn(identity);

builder.AddProject("ReLiveWP.Zune.Catalog");

builder.AddProject("ReLiveWP.Zune.Commerce")
    .DependsOn(identity);

// builder.AddProject("ReLiveWP.Web.Server", "ReLiveWP.Web/ReLiveWP.Web.Server")
//     .DependsOn(identity);

builder.Build()
    .Run();