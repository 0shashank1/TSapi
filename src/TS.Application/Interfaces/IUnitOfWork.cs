namespace TS.Application.Interfaces;

/// <summary>
/// Groups repository writes into a single atomic commit. One unit of work
/// is scoped to the request, so every repository of an aggregate shares the
/// same underlying change tracker.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all staged changes in one transaction.
    /// Implementations translate persistence concurrency failures into
    /// <c>ConcurrencyConflictException</c>.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
