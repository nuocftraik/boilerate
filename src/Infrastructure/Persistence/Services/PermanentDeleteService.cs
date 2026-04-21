using Boilerate.Application.Common.Persistence;
using Boilerate.Domain.Common.Contracts;
using Boilerate.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Boilerate.Infrastructure.Persistence.Services;

public class PermanentDeleteService<T> : IPermanentDeleteService<T> where T : class, IAggregateRoot
{
    private readonly ApplicationDbContext _dbContext;

    public PermanentDeleteService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task PermanentDeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        // Sử dụng ExecuteDeleteAsync để bỏ qua hoàn toàn các Interceptor (Soft Delete logic) trong SaveChangesAsync.
        // Đây là cách duy nhất để thực sự xóa vật lý (Physical Delete) khi BaseDbContext đang ép buộc Soft Delete.
        if (entity is IEntity<Guid> guidEntity)
        {
            // ExecuteDeleteAsync thực hiện lệnh DELETE trực tiếp trên SQL, không qua ChangeTracker.
            // Cần IgnoreQueryFilters vì bản ghi muốn xóa cứng thường là bản ghi đã xóa mềm (DeletedOn != null).
            await _dbContext.Set<T>()
                .IgnoreQueryFilters()
                .Where(x => EF.Property<Guid>(x, "Id") == guidEntity.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            // Fallback: Nếu không phải Guid Id, ta thử set state và save
            _dbContext.Entry(entity).State = EntityState.Deleted;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
