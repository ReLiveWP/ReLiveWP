using System.Text.Json;
using Atom.Formatters;
using Microsoft.AspNetCore.Authentication;
using ReLiveWP.Identity;
using ReLiveWP.Identity.Grpc;
using ReLiveWP.Services.Activity.Endpoints;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.ServiceDefaults.Outbound;
using ReLiveWP.Services.Activity.Providers.Bluesky;
using ReLiveWP.Services.Activity.Providers.Mastodon;
using ReLiveWP.Services.Activity.Services;
using ReLiveWP.Services.Activity.Utilities;
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

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
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

builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, MediaTicketAuthHandler>(MediaTicketAuthHandler.SchemeName, null);

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
builder.Services.AddGuardedOutboundClient();
builder.Services.AddSingleton<PublicActivityProviderBase, PublicBlueskyActivityProvider>();
builder.Services.AddSingleton<MastodonActorResolver>();
builder.Services.AddSingleton<PublicMastodonActivityProvider>();
builder.Services.AddSingleton<PublicActivityProviderBase>(s => s.GetRequiredService<PublicMastodonActivityProvider>());
builder.Services.AddSingleton<IOwnedActivityProviderFactory, BlueskyActivityProviderFactory>();
builder.Services.AddSingleton<IOwnedActivityProviderFactory, MastodonActivityProviderFactory>();
builder.Services.AddSingleton<ThumbnailService>();
builder.Services.AddSingleton(new MediaTicketService(builder.Configuration, TimeProvider.System));
builder.Services.AddScoped<SocialAlbumProviderBase, BlueskyAlbumProvider>();
builder.Services.AddScoped<SocialAlbumsService>();
builder.Services.AddScoped<ActivityProviderService>();
builder.Services.AddScoped<ActivityFeedReader>();
builder.Services.AddScoped<FeedRendererService>();
builder.Services.AddScoped<ConnectionLookupService>();
builder.Services.AddScoped<FileViewerService>();
builder.Services.AddScoped<PhotoLibraryService>();
builder.Services.AddScoped<SocialAlbumService>();
builder.Services.AddScoped<PhotoUploadService>();
builder.Services.AddScoped<PhotoStreamService>();
builder.Services.AddScoped<AlbumRenderService>();
builder.Services.AddScoped<WebAlbumsService>();

var app = builder.Build();

app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapSocialEndpoints();
app.MapAlbumEndpoints();
app.MapDefaultEndpoints();
app.Run();
