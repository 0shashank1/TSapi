namespace TS.Application.Common;

/// <summary>
/// Decoded keyset (seek) cursor: the sort value of the last row of a page
/// plus its id, used to resume an ordered query without OFFSET.
/// </summary>
public abstract record Keyset(Guid Id);

public sealed record DateKeyset(DateTime Value, Guid Id) : Keyset(Id);

public sealed record NumberKeyset(long Value, Guid Id) : Keyset(Id);

public sealed record StringKeyset(string Value, Guid Id) : Keyset(Id);
