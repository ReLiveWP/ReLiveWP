using Microsoft.AspNetCore.Mvc.Formatters;
using ReLiveWP.Identity;
using ReLiveWP.Backend.Identity;
using ReLiveWP.Zune;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLiveIDAuthentication(o =>
{
    o.GrpcConfiguration = (c) => c.Address = new Uri("http://127.0.0.4:5000");
    o.LiveIDConfiguration = (c) => c.ValidServiceTargets = ["live.zune.net", "live.xbox.com"];
});

builder.Services.AddControllers(o =>
{
    // modCheck WithDefaultOutputFormatter? no?
    o.OutputFormatters.Insert(0, new ZestOutputFormatter());
    o.InputFormatters.Insert(0, new ZestInputFormatter(o));
});

builder.Services.AddGrpcClient<Authentication.AuthenticationClient>(
    o => o.Address = new Uri("http://127.0.0.4:5000"));

builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri("http://127.0.0.4:5000"));


var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
