using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceRegistration;
using ReLiveWP.Backend.DeviceRegistration.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DevicesDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<DeviceRegistrationService>();

app.Run();
