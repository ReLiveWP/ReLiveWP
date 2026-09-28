using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ReLiveWP.Backend.ConnectedServices.Data;

// the token converter bakes the protector into the model, and EF caches models per context type
public class ConnectedServicesModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is not ConnectedServicesDbContext connectedServices)
            return (context.GetType(), designTime);

        return (context.GetType(), connectedServices.TokenProtector, designTime);
    }
}
