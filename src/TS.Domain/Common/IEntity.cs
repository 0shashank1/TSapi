namespace TS.Domain.Common;

/// <summary>
/// Identity contract for persistable entities whose primary key is
/// <see cref="Id"/>. Lets generic persistence abstractions address any
/// entity without forcing a shared base class.
/// </summary>
public interface IEntity
{
    Guid Id { get; }
}
