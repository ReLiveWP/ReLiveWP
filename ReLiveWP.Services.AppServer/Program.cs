var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddControllers();

var app = builder.Build();

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

app.MapDefaultEndpoints();

app.Run();
