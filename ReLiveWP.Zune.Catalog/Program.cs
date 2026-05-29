var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.Run();
