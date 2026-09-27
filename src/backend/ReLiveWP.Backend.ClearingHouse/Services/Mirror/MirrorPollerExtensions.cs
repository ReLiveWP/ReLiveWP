namespace ReLiveWP.Backend.ClearingHouse.Services.Mirror;

public static class MirrorPollerExtensions
{
    public static IServiceCollection AddMirrorPoller(this IServiceCollection services, MirrorKind kind) =>
        services.AddSingleton<IHostedService>(sp => ActivatorUtilities.CreateInstance<MirrorPoller>(sp, kind));
}
