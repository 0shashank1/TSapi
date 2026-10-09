using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TS.Application.Interfaces;
using TS.Domain.Common;

namespace TS.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core backed generic repository. All instances share the scoped
/// <see cref="TSDbContext"/>, so staged writes are visible to every
/// repository until <see cref="IUnitOfWork.SaveChangesAsync"/> commits them.
/// </summary>
public class EfRepository<TEntity>(TSDbContext dbContext) : IRepository<TEntity>
    where TEntity : class, IEntity
{
    protected TSDbContext DbContext { get; } = dbContext;

    protected DbSet<TEntity> DbSet => DbContext.Set<TEntity>();

    public virtual Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => DbSet.FindAsync([id], cancellationToken).AsTask();

    public virtual async Task<TEntity?> GetByIdAsNoTrackingAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    public virtual async Task<List<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = DbSet.AsNoTracking();

        if (predicate is not null)
            query = query.Where(predicate);

        return await query.ToListAsync(cancellationToken);
    }

    public virtual Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().AnyAsync(predicate, cancellationToken);

    public virtual Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => predicate is null
            ? DbSet.AsNoTracking().CountAsync(cancellationToken)
            : DbSet.AsNoTracking().CountAsync(predicate, cancellationToken);

    public virtual void Add(TEntity entity)
        => DbSet.Add(entity);

    public virtual void AddRange(IEnumerable<TEntity> entities)
        => DbSet.AddRange(entities);

    public virtual void Update(TEntity entity)
        => DbSet.Update(entity);

    public virtual void Remove(TEntity entity)
        => DbSet.Remove(entity);

    /// <summary>
    /// Set-based delete of at most <paramref name="limit"/> matching rows —
    /// no entities are materialized, and cascades are left to the database.
    /// Returns the number of rows removed.
    /// </summary>
    protected async Task<int> DeleteBatchAsync(
        IQueryable<TEntity> matches,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var ids = await matches
            .Take(limit)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
            return 0;

        return await DbSet
            .Where(entity => ids.Contains(entity.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
