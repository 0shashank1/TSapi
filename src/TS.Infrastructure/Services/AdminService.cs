using Microsoft.Extensions.Logging;
using TS.Application.Common;
using TS.Application.DTOs.Admin;
using TS.Application.Interfaces;
using TS.Domain.Enums;

namespace TS.Infrastructure.Services;

public sealed class AdminService : IAdminService
{
    private readonly IUserRepository _users;
    private readonly ISnippetRepository _snippets;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IShareAccessLogRepository _accessLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        IUserRepository users,
        ISnippetRepository snippets,
        IRefreshTokenRepository refreshTokens,
        IShareAccessLogRepository accessLogs,
        IUnitOfWork unitOfWork,
        ILogger<AdminService> logger)
    {
        _users = users;
        _snippets = snippets;
        _refreshTokens = refreshTokens;
        _accessLogs = accessLogs;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResponse<AdminUserListItem>> ListUsersAsync(
        AdminUserListQuery query,
        CancellationToken cancellationToken = default)
    {
        var isActive = ParseStatus(query.Status);
        var role = ParseRole(query.Role);
        var cursor = ParseDateCursor(query.Cursor);

        var rows = await _users.SearchAsync(
            query.Search,
            isActive,
            role,
            cursor,
            query.PageSize + 1,
            cancellationToken);

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
        var user = await _users.GetByIdAsNoTrackingAsync(
            userId,
            cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        var now = DateTime.UtcNow;

        var snippetCount = await _snippets.CountByOwnerAsync(
            userId,
            cancellationToken);

        var activeRefreshTokenCount = await _refreshTokens.CountActiveAsync(
            userId,
            now,
            cancellationToken);

        return AdminUserResponse.From(user, snippetCount, activeRefreshTokenCount);
    }

    public async Task SetStatusAsync(
        Guid userId,
        AdminUserStatusRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        var now = DateTime.UtcNow;

        if (!request.IsActive)
        {
            user.Deactivate(now);

            var activeTokens = await _refreshTokens.ListActiveByUserAsync(
                userId,
                cancellationToken);

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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetRoleAsync(
        Guid userId,
        AdminUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        var role = ParseRole(request.Role)
            ?? throw InvalidFilter("role", "Must be one of: user, admin.");

        if (user.Role != role)
        {
            user.SetRole(role, DateTime.UtcNow);

            // Role changes are security-sensitive: audit them.
            _logger.LogInformation(
                "Admin changed role of user {UserId} to {Role} at {Timestamp:O}",
                userId,
                role,
                DateTime.UtcNow);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<PagedResponse<AccessLogListItem>> ListAccessLogsAsync(
        AccessLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var cursor = ParseDateCursor(query.Cursor);

        var rows = await _accessLogs.SearchAsync(
            query.Success,
            query.Reason,
            query.From,
            query.To,
            cursor,
            query.PageSize + 1,
            cancellationToken);

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
        var status = ParseTokenStatus(query.Status);
        var cursor = ParseDateCursor(query.Cursor);

        var rows = await _refreshTokens.SearchAsync(
            query.UserId,
            status,
            cursor,
            query.PageSize + 1,
            cancellationToken);

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

    private static bool? ParseStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();

        return normalized switch
        {
            null or "" or "all" => null,
            "active" => true,
            "inactive" => false,
            _ => throw InvalidFilter(
                "status", "Must be one of: active, inactive, all.")
        };
    }

    private static UserRole? ParseRole(string? role)
    {
        var normalized = role?.Trim().ToLowerInvariant();

        return normalized switch
        {
            null or "" or "all" => null,
            "user" => UserRole.User,
            "admin" => UserRole.Admin,
            _ => throw InvalidFilter("role", "Must be one of: user, admin.")
        };
    }

    private static RefreshTokenStatus ParseTokenStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();

        return normalized switch
        {
            null or "" or "all" => RefreshTokenStatus.All,
            "active" => RefreshTokenStatus.Active,
            "revoked" => RefreshTokenStatus.Revoked,
            _ => throw InvalidFilter(
                "status", "Must be one of: active, revoked, all.")
        };
    }

    private static DateKeyset? ParseDateCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        if (!Cursor.TryDecodeDateTime(cursor, out var sortValue, out var id))
            throw InvalidCursor();

        return new DateKeyset(sortValue, id);
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
