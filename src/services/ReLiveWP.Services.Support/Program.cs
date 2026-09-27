using ReLiveWP.Services.Support.Services;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<KbContentStore>();
builder.Services.AddSingleton<FwlinkStore>();
builder.Services.AddSingleton<AreaStore>();

var app = builder.Build();

app.UseStaticFiles();
app.MapControllers();
app.MapDefaultEndpoints();

ContentValidation.Run(app.Services, app.Logger);

app.Run();
