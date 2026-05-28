using Atom.Formatters;
using ReLiveWP.Identity;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCompression((o) =>
{
    o.MimeTypes = ["application/atom+xml", .. o.MimeTypes];
});

builder.Services.AddControllers(c =>
{
    c.InputFormatters.Clear();
    c.InputFormatters.Add(new AtomInputFormatter(c));
    c.OutputFormatters.Clear();
    c.OutputFormatters.Add(new AtomOutputFormatter());
});

builder.Services.AddLiveIDAuthentication((o) =>
{
    o.IdentityGrpcConfiguration = (c) => c.Address = new Uri(builder.Configuration["Endpoints:Identity"]!);
    o.ConnectedServicesGrpcConfiguration = c => c.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!);
    o.LiveIDConfiguration = (c) => c.ValidServiceTargets = ["http://Passport.NET/tb", "relivewp.net", "spaces.int.relivewp.net", "spaces.relivewp.net"];
});

builder.Services.AddGrpcClient<Authentication.AuthenticationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));
builder.Services.AddGrpcClient<ConnectedServices.ConnectedServicesClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!));
builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ActivityProviderService>();

var app = builder.Build();

app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
