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
var registration = builder.AddProject("ReLiveWP.Backend.DeviceRegistration");
var deviceUpdate = builder.AddProject("ReLiveWP.Backend.DeviceUpdate");

var connectedServices = builder.AddProject("ReLiveWP.Backend.ConnectedServices")
    .DependsOn(identity);

var mailbox = builder.AddProject("ReLiveWP.Backend.Mailbox")
    .DependsOn(identity);

var skybox = builder.AddProject("ReLiveWP.Backend.Skybox")
    .DependsOn(identity);

var skydrive = builder.AddProject("ReLiveWP.Backend.SkyDrive")
    .DependsOn(identity)
    .DependsOn(connectedServices);

builder.AddProject("ReLiveWP.Services.Activation")
    .DependsOn(registration);

builder.AddProject("ReLiveWP.Services.Login")
    .DependsOn(identity)
    .DependsOn(connectedServices)
    .DependsOn(registration);

builder.AddProject("ReLiveWP.Services.Activity")
    .DependsOn(identity)
    .DependsOn(connectedServices)
    .DependsOn(skydrive);

builder.AddProject("ReLiveWP.Services.Push")
    .DependsOn(identity);

builder.AddProject("ReLiveWP.Services.AddressBook")
    .DependsOn(identity)
    .DependsOn(mailbox);

builder.AddProject("ReLiveWP.Services.Exchange")
    .DependsOn(identity)
    .DependsOn(mailbox);

builder.AddProject("ReLiveWP.Services.FindMyPhone")
    .DependsOn(skybox)
    .DependsOn(identity);

builder.AddProject("ReLiveWP.Services.Devices")
    .DependsOn(skybox)
    .DependsOn(identity);

builder.AddProject("ReLiveWP.Services.Orion");

builder.AddProject("ReLiveWP.Services.Profile")
    .DependsOn(identity)
    .DependsOn(mailbox);

builder.Build()
    .Run();