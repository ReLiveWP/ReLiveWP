using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceRegistration.Certificates;
using ReLiveWP.Backend.DeviceRegistration.Data;
using ReLiveWP.Backend.DeviceRegistration.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<ICertificateService, WindowsPhoneCertificateService>();
builder.Services.AddSingleton<RootCACertificateProvider>();
AddActivationCodeValidator(builder);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DevicesDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddReadinessCheck("postgres", (sp, ct) => sp.GetRequiredService<DevicesDbContext>().Database.CanConnectAsync(ct));

var app = builder.Build();

ApplyMigrations(app);

// Configure the HTTP request pipeline.
app.MapGrpcService<ClientProvisioningService>();
app.MapGrpcService<DeviceRegistrationService>();

app.MapDefaultEndpoints();

app.Run();

static void AddActivationCodeValidator(WebApplicationBuilder builder)
{
#if PRODUCT_KEYS
    builder.Services.AddSingleton<IActivationCodeValidator, ProductKeyActivationCodeValidator>();
#else
    if (builder.Configuration.GetValue<bool>("ProductKeys:Required"))
        throw new InvalidOperationException("ProductKeys:Required is set but this build has no product key validator, check private/ was present at build time.");

    builder.Services.AddSingleton<IActivationCodeValidator, PermissiveActivationCodeValidator>();
#endif
}

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    using var dbContext = scope.ServiceProvider.GetRequiredService<DevicesDbContext>();

    dbContext.Database.Migrate();
}