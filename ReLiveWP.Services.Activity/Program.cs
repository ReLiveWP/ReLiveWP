using Atom.Formatters;
using ReLiveWP.Identity;
using ReLiveWP.Identity.Grpc;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Grpc;
using ReLiveWP.Services.Grpc.Mailbox;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuthForwardingInterceptor>();

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
    o.ConnectedServicesGrpcConfiguration = c => c.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!);
    o.LiveIDConfiguration = (c) => c.ValidServiceTargets = [
        "http://Passport.NET/tb",
        "relivewp.net", 
        "spaces.int.relivewp.net",
        "spaces.relivewp.net", 
        "skydrive.int.relivewp.com", // oops! 
        "skydrive.relivewp.com", // oops!
        "skydrive.int.relivewp.net",
        "skydrive.relivewp.net",
    ];
});

builder.Services.AddGrpcClient<Authentication.AuthenticationClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));
builder.Services.AddGrpcClient<ConnectedServices.ConnectedServicesClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:ConnectedServices:Grpc"]!))
    .AddInterceptor<AuthForwardingInterceptor>();
builder.Services.AddGrpcClient<User.UserClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Identity"]!));
builder.Services.AddGrpcClient<MailboxStore.MailboxStoreClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:Mailbox"]!));
builder.Services.AddGrpcClient<SkyDrive.SkyDriveClient>(
    o => o.Address = new Uri(builder.Configuration["Endpoints:SkyDrive"]!))
    .AddInterceptor<AuthForwardingInterceptor>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ThumbnailResizer>();
builder.Services.AddScoped<SocialAlbumProviderBase, BlueskyAlbumProvider>();
builder.Services.AddScoped<SocialAlbums>();
builder.Services.AddScoped<ActivityProviderService>();
builder.Services.AddScoped<FeedRenderer>();
builder.Services.AddScoped<ConnectionLookup>();
builder.Services.AddScoped<FilesViewer>();
builder.Services.AddScoped<PhotoLibraryService>();
builder.Services.AddScoped<SocialAlbumService>();
builder.Services.AddScoped<PhotoUploadService>();
builder.Services.AddScoped<PhotoStreamService>();

var app = builder.Build();

app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();
