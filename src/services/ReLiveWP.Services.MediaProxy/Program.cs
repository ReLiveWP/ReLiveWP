using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.ServiceDefaults.Outbound;
using ReLiveWP.Services.MediaProxy.Endpoints;
using ReLiveWP.Services.MediaProxy.Services;
using ReLiveWP.Services.MediaProxy.Utilities;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceEndpoints();

var signer = MediaProxyUrlSigner.FromConfiguration(builder.Configuration);

builder.Services.AddSingleton<IOutboundAddressPolicy, PublicOnlyAddressPolicy>();
builder.Services.AddSingleton(signer);
builder.Services.AddSingleton<ImagePipelineService>();
builder.Services.AddSingleton(sp =>
{
    var remote = RemoteMediaService.CreateRemoteClient(sp.GetRequiredService<IOutboundAddressPolicy>());
    return ActivatorUtilities.CreateInstance<RemoteMediaService>(sp, remote);
});

var app = builder.Build();

var publicPort = KestrelEndpointPorts.ReadEndpointPort(app.Configuration, KestrelEndpointPorts.PublicEndpoint);
var internalPort = KestrelEndpointPorts.ReadEndpointPort(app.Configuration, KestrelEndpointPorts.InternalEndpoint);
if (publicPort == internalPort)
    throw new InvalidOperationException("The public and internal endpoints must listen on different ports.");

if (!signer.CanVerify)
    app.Logger.LogWarning("No {Section} configured, every public request will 404", MediaProxyUrlSigner.KeysSection);

app.MapDefaultEndpoints();
app.MapInternalEndpoints(internalPort);
app.MapPublicEndpoints();

app.Run();
