using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ReLiveWP.Backend.Identity.Data;

public class LiveDbContext : IdentityDbContext<LiveUser, LiveRole, Guid>
{
    public LiveDbContext(DbContextOptions<LiveDbContext> options)
        : base(options)
    {
    }
}
