using Boilerate.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Boilerate.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Permission system DbSets
    public DbSet<Domain.Identity.Action> Actions => Set<Domain.Identity.Action>();
    public DbSet<Function> Functions => Set<Function>();
    public DbSet<ActionInFunction> ActionInFunctions => Set<ActionInFunction>();
    public DbSet<Permission> Permissions => Set<Permission>();
}
