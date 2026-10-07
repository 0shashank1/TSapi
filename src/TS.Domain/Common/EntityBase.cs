namespace TS.Domain.Common;

public abstract class EntityBase
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; protected set; } = DateTime.UtcNow;

    // Application-managed optimistic concurrency token.
    public Guid Version { get; protected set; } = Guid.NewGuid();



    protected void Touch(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
        Version = Guid.NewGuid();
    }
}
