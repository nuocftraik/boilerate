using Boilerate.Application.Common.Events;
using Boilerate.Application.Common.Interfaces;
using Boilerate.Domain.Common.Contracts;
using Boilerate.Domain.Identity;
using Boilerate.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Boilerate.Infrastructure.Persistence.Context;

/// <summary>
/// Base DbContext với domain events và soft delete support.
/// </summary>
public abstract class BaseDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    string,
    IdentityUserClaim<string>,
    IdentityUserRole<string>,
    IdentityUserLogin<string>,
    ApplicationRoleClaim,
    IdentityUserToken<string>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEventPublisher _events;

    protected BaseDbContext(
        DbContextOptions options,
        ICurrentUser currentUser,
        IEventPublisher events)
        : base(options)
    {
        _currentUser = currentUser;
        _events = events;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // QueryFilters need to be applied before base.OnModelCreating
        modelBuilder.AppendGlobalQueryFilter<ISoftDelete>(e => e.DeletedOn == null);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Enable logging cho development
        optionsBuilder.EnableSensitiveDataLogging();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Handle auditing (Created/Modified/Deleted tracking) và soft delete
        HandleAuditingBeforeSaveChanges();

        // Publish domain events
        await PublishDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Handle auditing (Created/Modified/Deleted tracking) và soft delete.
    /// </summary>
    private void HandleAuditingBeforeSaveChanges()
    {
        var userId = _currentUser.GetUserId();

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.CreatedOn = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = userId;
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    // ⭐ SOFT DELETE LOGIC ⭐
                    // Thay vì xóa vật lý, chuyển sang Modified và set DeletedOn/DeletedBy
                    if (entry.Entity is ISoftDelete softDelete)
                    {
                        softDelete.DeletedOn = DateTime.UtcNow;
                        softDelete.DeletedBy = userId;
                        entry.State = EntityState.Modified;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Publish domain events qua MediatR.
    /// </summary>
    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        var entitiesWithEvents = ChangeTracker
            .Entries<IEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        entitiesWithEvents.ForEach(e => e.Entity.DomainEvents.Clear());

        foreach (var domainEvent in domainEvents)
        {
            await _events.PublishAsync(domainEvent);
        }
    }
}
