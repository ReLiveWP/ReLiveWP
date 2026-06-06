var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.MapControllerRoute(
  name: "areas",
  pattern: "{area:exists}Service/{controller}Service.svc/{action=Index}/{id?}"
);

app.MapControllerRoute(
  name: "areas_with_format",
  pattern: "{area:exists}Service/{controller}Service.svc/{format?}/{action=Index}/{id?}"
);

app.Run();
