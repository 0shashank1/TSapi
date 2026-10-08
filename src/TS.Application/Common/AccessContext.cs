namespace TS.Application.Common;

/// <summary>
/// Thin wrapper around the authenticated principal's identity so
/// application services never depend on ASP.NET Core types.
/// </summary>
public sealed record AccessContext(Guid UserId, bool IsAdmin);
