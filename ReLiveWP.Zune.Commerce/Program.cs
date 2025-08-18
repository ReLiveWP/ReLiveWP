using ReLiveWP.Identity;
using ReLiveWP.Zune;
using ReLiveWP.Services.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLiveIDAuthentication(o =>
{
    o.GrpcConfiguration = (c) => c.Address = new Uri(builder.Configuration["Endpoints:Identity"]!);
    o.LiveIDConfiguration = (c) => c.ValidServiceTargets = ["live.zune.net", "live.xbox.com", "kdc.xboxlive.com"];
});

builder.Services.AddControllers(o =>
{
    // modCheck WithDefaultOutputFormatter? no?
    o.OutputFormatters.Insert(0, new ZestOutputFormatter());
    o.InputFormatters.Insert(0, new ZestInputFormatter(o));
});

builder.Services.AddGrpcClient<Authentication.AuthenticationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));

builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:User"]!));

var app = builder.Build();

app.Use(async (ctx, next) =>
{
    ctx.Request.EnableBuffering(); // this is dumb
    await next();
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
