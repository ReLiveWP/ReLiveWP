using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReLiveWP.Backend.Certificates;
using ReLiveWP.Backend.ClientProvisioning;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<ICertificateService, WP7CertificateService>();
builder.Services.AddSingleton<RootCACertificateProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ClientProvisioningService>();

app.Run();
