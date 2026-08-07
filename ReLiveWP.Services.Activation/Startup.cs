using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.DeviceRegistration;

namespace ReLiveWP.Services.Activation;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddDefaultHealthChecks();

        services.AddGrpcClient<DeviceRegistration.DeviceRegistrationClient>(
            o => o.Address = new Uri(Configuration["Endpoints:DeviceRegistration"]!));
        services.AddGrpcClient<ClientProvisioning.ClientProvisioningClient>(
            o => o.Address = new Uri(Configuration["Endpoints:ClientProvisioning"]!));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapDefaultHealthCheckEndpoints();
        });
    }
}
