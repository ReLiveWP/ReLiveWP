using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.ConnectedServices.Data;
using ReLiveWP.Backend.ConnectedServices.Providers;
using ReLiveWP.Backend.ConnectedServices.Proxy;
using ReLiveWP.Backend.ConnectedServices.Services;
using ReLiveWP.Identity;

namespace ReLiveWP.Backend.ConnectedServices;

public sealed class ConnectedServicesProxyLog;

public static class ConnectedServicesProxy
{
    public static void MapConnectedServicesProxy(this WebApplication app)
    {
        app.Map("/proxy/{serviceId}/{**path}", ProxyHandler);
        app.Map("/xrpc/{**path}", XRpcProxyHandler);
    }


    private static Task XRpcProxyHandler(HttpContext context,
                                         ConnectedServicesDbContext dbContext,
                                         ILogger<ConnectedServicesProxyLog> logger,
                                         ServiceTokenLocks tokenLocks,
                                         string? path)
        => ProxyHandler(context, dbContext, logger, tokenLocks, AtProto.SERVICE_NAME, path);

    private static async Task ProxyHandler(HttpContext context,
                                           ConnectedServicesDbContext dbContext,
                                           ILogger<ConnectedServicesProxyLog> logger,
                                           ServiceTokenLocks tokenLocks,
                                           string serviceId,
                                           string? path)
    {
        // TODO: probably best to escape more of this
        path = (path ?? string.Empty).Replace("#", "%23");

        try
        {
            context.Request.EnableBuffering();

            var userId = GetUserId(context);
            var connectionId = GetConnectionId(context);

            logger.LogInformation("Proxying /{ServiceId}/{Path}", serviceId, path);

            var service = await LoadServiceAsync(dbContext, userId, connectionId, serviceId);
            if (service == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var proxy = context.RequestServices.GetKeyedService<IConnectedServiceProxy>(serviceId);
            if (proxy == null)
            {
                logger.LogWarning("No proxy registered for service {ServiceId}", serviceId);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (service.IsDueForRefresh)
            {
                var serviceLock = await tokenLocks.AcquireAsync(service.Id, context.RequestAborted);
                try
                {
                    if (!serviceLock.IsAcquired)
                    {
                        logger.LogError("Failed to acquire lock on {ServiceId}!", service.Id);
                        context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
                        return;
                    }

                    service = await LoadServiceAsync(dbContext, userId, connectionId, serviceId);
                    if (service == null)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    // reload in case EF cached our entity between refetches
                    await dbContext.Entry(service).ReloadAsync(context.RequestAborted);
                    if (dbContext.Entry(service).State == EntityState.Detached)
                    {
                        // it got deleted
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    // recheck Busted after reload, DB state may have changed while we waited for the lock
                    if ((service.Flags & LiveConnectedServiceFlags.Busted) == LiveConnectedServiceFlags.Busted)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    if (service.IsDueForRefresh)
                    {
                        logger.LogInformation("Service {ConnectionId} requires refresh", service.Id);

                        var refreshed = await proxy.RefreshAsync(service, context.RequestAborted);
                        service.ApplyRefreshResult(refreshed);

                        await dbContext.SaveChangesOverConcurrentWritesAsync(context.RequestAborted);
                    }

                    if ((service.Flags & LiveConnectedServiceFlags.Busted) == LiveConnectedServiceFlags.Busted)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }
                }
                finally
                {
                    await serviceLock.DisposeAsync();
                }
            }


            await proxy.SendProxiedRequestAsync(service, context, path, context.RequestAborted);

            if ((service.Flags & LiveConnectedServiceFlags.NeedsRefresh) == LiveConnectedServiceFlags.NeedsRefresh)
            {
                await dbContext.ConnectedServices
                    .Where(s => s.Id == service.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Flags, c => c.Flags | LiveConnectedServiceFlags.NeedsRefresh),
                                        context.RequestAborted);
            }

            logger.LogInformation("Proxied /{ServiceId}/{Path} for {ConnectionId}", serviceId, path, service.Id);
        }
        catch (OperationCanceledException) { /* client disconnected */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to proxy request to {ServiceId}/{Path}", serviceId, path);
            if (!context.Response.HasStarted)
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }

    private static async Task<LiveConnectedService?> LoadServiceAsync(
        ConnectedServicesDbContext dbContext, Guid userId, Guid connectionId, string serviceId)
    {
        var service = await dbContext.ConnectedServices.FirstOrDefaultAsync(s => s.Id == connectionId);

        if (service == null ||
            service.Service != serviceId ||
            service.UserId != userId ||
            (service.Flags & LiveConnectedServiceFlags.Busted) == LiveConnectedServiceFlags.Busted)
            return null;

        return service;
    }

    private static Guid GetUserId(HttpContext context)
    {
        var sub = context.User.Id()
            ?? throw new InvalidOperationException("No user identity on request.");
        return Guid.Parse(sub);
    }

    private static Guid GetConnectionId(HttpContext context)
    {
        var header = context.Request.Headers["X-Connection-ID"].ToString();
        if (!Guid.TryParse(header, out var id))
            throw new InvalidOperationException("Missing or invalid X-Connection-ID header.");
        return id;
    }
}
