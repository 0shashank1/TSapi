using Microsoft.EntityFrameworkCore;
using TS.Application.Common;
using TS.Application.Interfaces;

namespace TS.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work: commits every staged repository change in a single
/// transaction and maps optimistic-concurrency failures to the application's
/// <see cref="ConcurrencyConflictException"/>.
/// </summary>
public sealed class UnitOfWork(TSDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "The resource was changed by another request.");
        }
    }
}
