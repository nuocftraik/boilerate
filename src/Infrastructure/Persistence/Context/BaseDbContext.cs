using Boilerate.Application.Common.Events;
using Boilerate.Application.Common.Interfaces;
using Boilerate.Domain.Auditing;
using Boilerate.Domain.Common.Contracts;
using Boilerate.Domain.Identity;
using Boilerate.Infrastructure.Auditing;
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
    private readonly ISerializerService _serializer;
    private readonly IEventPublisher _events;

    protected BaseDbContext(
        DbContextOptions options,
        ICurrentUser currentUser,
        ISerializerService serializer,
        IEventPublisher events)
        : base(options)
    {
        _currentUser = currentUser;
        _serializer = serializer;
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
        // 1. Handle soft delete and auditing metadata (CreatedOn, DeletedOn, etc.)
        HandleAuditingMetadataBeforeSaveChanges();

        // Ensure EF Core captures all changes before we analyze them for Audit Trail
        ChangeTracker.DetectChanges();

        // 2. Gather audit entries (Giai đoạn chuẩn bị)
        var auditEntries = AuditTrailHelper.GatherAuditEntries(ChangeTracker, _currentUser.GetUserId(), _serializer);

        // 3. Publish domain events
        await PublishDomainEventsAsync(cancellationToken);

        // 4. Save audit entries that don't depend on DB-generated values
        foreach (var entry in auditEntries.Where(e => !e.HasTemporaryProperties))
        {
            Set<Trail>().Add(entry.ToAuditTrail());
        }

        // 5. First Save: Lưu dữ liệu chính và các Audit Logs không phụ thuộc ID DB
        int result = await base.SaveChangesAsync(cancellationToken);

        // 6. Handle Audit entries with temporary properties (Giai đoạn hoàn tất)
        // Những record mới thêm có ID do DB sinh ra sẽ được xử lý ở đây
        if (auditEntries.Any(e => e.HasTemporaryProperties))
        {
            foreach (var entry in auditEntries.Where(e => e.HasTemporaryProperties))
            {
                foreach (var prop in entry.TemporaryProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        entry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    else
                    {
                        entry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                Set<Trail>().Add(entry.ToAuditTrail());
            }

            // Lưu nốt phần Audit Logs còn lại
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Handle soft delete logic và cập nhật audit metadata (CreatedOn, LastModifiedOn, etc.).
    /// </summary>
    private void HandleAuditingMetadataBeforeSaveChanges()
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
