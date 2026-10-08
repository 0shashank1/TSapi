using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TS.Application.Common;
using TS.Application.DTOs.Admin;
using TS.Application.Interfaces;
using TS.Domain.Entities;
using TS.Domain.Enums;
using TS.Infrastructure.Persistence;

namespace TS.Infrastructure.Services;

public sealed class AdminService : IAdminService
{
    private readonly TSDbContext _db;
    private readonly ILogger<AdminService> _logger;

    public AdminService(TSDbContext db, ILogger<AdminService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResponse<AdminUserListItem>> ListUsersAsync(
        AdminUserListQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> source = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(u =>
                u.Email.Contains(term) ||
                (u.DisplayName != null && u.DisplayName.Contains(term)));
        }

        var status = query.Status?.Trim().ToLowerInvariant();
        source = status switch
        {
            null or "" or "all" => source,
            "active" => source.Where(u => u.IsActive),
            "inactive" => source.Where(u => !u.IsActive),
            _ => throw InvalidFilter(
                "status", "Must be one of: active, inactive, all.")
        };

        var role = query.Role?.Trim().ToLowerInvariant();
        source = role switch
        {
            null or "" or "all" => source,
            "user" => source.Where(u => u.Role == UserRole.User),
            "admin" => source.Where(u => u.Role == UserRole.Admin),
            _ => throw InvalidFilter("role", "Must be one of: user, admin.")
        };

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!TryDecodeCursor(query.Cursor, out var lastCreatedAt, out var lastId))
                throw InvalidCursor();

            source = source.Where(u =>
                u.CreatedAtUtc < lastCreatedAt ||
                (u.CreatedAtUtc == lastCreatedAt && u.Id.CompareTo(lastId) < 0));
        }

        var rows = await source
            .OrderByDescending(u => u.CreatedAtUtc)
            .ThenByDescending(u => u.Id)
            .Take(query.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > query.PageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = Cursor.Encode(last.CreatedAtUtc.Ticks, last.Id);
        }

        return new PagedResponse<AdminUserListItem>(
            rows.Select(AdminUserListItem.From).ToList(),
            nextCursor);
    }

    public async Task<AdminUserResponse> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        var now = DateTime.UtcNow;

        var snippetCount = await _db.TextSnippets.CountAsync(
            s => s.OwnerUserId == userId,
            cancellationToken);

        var activeRefreshTokenCount = await _db.RefreshTokens.CountAsync(
            t => t.UserId == userId &&
                 t.RevokedAtUtc == null &&
                 t.ExpiresAtUtc > now,
            cancellationToken);

        return AdminUserResponse.From(user, snippetCount, activeRefreshTokenCount);
    }

    public async Task SetStatusAsync(
        Guid userId,
        AdminUserStatusRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        var now = DateTime.UtcNow;

        if (!request.IsActive)
        {
            user.Deactivate(now);

            var activeTokens = await _db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.Revoke(now, "AdminDeactivated", ipAddress);
            }

            _logger.LogInformation(
                "Admin deactivated user {UserId} at {Timestamp:O}",
                userId,
                now);
        }
        else if (!user.IsActive)
        {
            user.Activate(now);

            _logger.LogInformation(
                "Admin activated user {UserId} at {Timestamp:O}",
                userId,
                now);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetRoleAsync(
        Guid userId,
        AdminUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        var role = request.Role.Trim().ToLowerInvariant() switch
        {
            "user" => UserRole.User,
            "admin" => UserRole.Admin,
            _ => throw InvalidFilter("role", "Must be one of: user, admin.")
        };

        if (user.Role != role)
        {
            user.SetRole(role, DateTime.UtcNow);

            // Role changes are security-sensitive: audit them.
            _logger.LogInformation(
                "Admin changed role of user {UserId} to {Role} at {Timestamp:O}",
                userId,
                role,
                DateTime.UtcNow);

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PagedResponse<AccessLogListItem>> ListAccessLogsAsync(
        AccessLogQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ShareAccessLog> source = _db.ShareAccessLogs.AsNoTracking();

        if (query.Success.HasValue)
            source = source.Where(l => l.WasSuccessful == query.Success.Value);

        if (!string.IsNullOrWhiteSpace(query.Reason))
        {
            var reason = query.Reason.Trim();
            source = source.Where(l => l.FailureReason == reason);
        }

        if (query.From.HasValue)
        {
            var from = query.From.Value.ToUniversalTime();
            source = source.Where(l => l.AccessedAtUtc >= from);
        }

        if (query.To.HasValue)
        {
            var to = query.To.Value.ToUniversalTime();
            source = source.Where(l => l.AccessedAtUtc < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!TryDecodeCursor(query.Cursor, out var lastAccessedAt, out var lastId))
                throw InvalidCursor();

            source = source.Where(l =>
                l.AccessedAtUtc < lastAccessedAt ||
                (l.AccessedAtUtc == lastAccessedAt && l.Id.CompareTo(lastId) < 0));
        }

        var rows = await source
            .OrderByDescending(l => l.AccessedAtUtc)
            .ThenByDescending(l => l.Id)
            .Take(query.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > query.PageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = Cursor.Encode(last.AccessedAtUtc.Ticks, last.Id);
        }

        return new PagedResponse<AccessLogListItem>(
            rows.Select(AccessLogListItem.From).ToList(),
            nextCursor);
    }

    public async Task<PagedResponse<RefreshTokenListItem>> ListRefreshTokensAsync(
        RefreshTokenQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<RefreshToken> source = _db.RefreshTokens.AsNoTracking();

        if (query.UserId.HasValue)
            source = source.Where(t => t.UserId == query.UserId.Value);

        var status = query.Status?.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        source = status switch
        {
            null or "" or "all" => source,
            "active" => source.Where(
                t => t.RevokedAtUtc == null && t.ExpiresAtUtc > now),
            "revoked" => source.Where(t => t.RevokedAtUtc != null),
            _ => throw InvalidFilter(
                "status", "Must be one of: active, revoked, all.")
        };

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!TryDecodeCursor(query.Cursor, out var lastCreatedAt, out var lastId))
                throw InvalidCursor();

            source = source.Where(t =>
                t.CreatedAtUtc < lastCreatedAt ||
                (t.CreatedAtUtc == lastCreatedAt && t.Id.CompareTo(lastId) < 0));
        }

        var rows = await source
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Take(query.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > query.PageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = Cursor.Encode(last.CreatedAtUtc.Ticks, last.Id);
        }

        return new PagedResponse<RefreshTokenListItem>(
            rows.Select(RefreshTokenListItem.From).ToList(),
            nextCursor);
    }

    private static bool TryDecodeCursor(
        string cursor,
        out DateTime sortValue,
        out Guid id)
    {
        sortValue = default;
        id = default;

        if (!Cursor.TryDecodeString(cursor, out var raw, out id) ||
            !long.TryParse(raw, out var ticks) ||
            ticks < DateTime.MinValue.Ticks ||
            ticks > DateTime.MaxValue.Ticks)
        {
            return false;
        }

        sortValue = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }

    private static ValidationException InvalidFilter(string field, string message)
        => new(
            $"Invalid {field} filter.",
            new Dictionary<string, string[]> { [field] = [message] });

    private static ValidationException InvalidCursor()
        => new(
            "Invalid cursor.",
            new Dictionary<string, string[]>
            {
                ["cursor"] = ["The cursor is malformed or from a different query."]
            });
}
