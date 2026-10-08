using TS.Application.Common;
using TS.Application.DTOs.Admin;

namespace TS.Application.Interfaces;

public interface IAdminService
{
    Task<PagedResponse<AdminUserListItem>> ListUsersAsync(
        AdminUserListQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminUserResponse> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task SetStatusAsync(
        Guid userId,
        AdminUserStatusRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task SetRoleAsync(
        Guid userId,
        AdminUserRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<AccessLogListItem>> ListAccessLogsAsync(
        AccessLogQuery query,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<RefreshTokenListItem>> ListRefreshTokensAsync(
        RefreshTokenQuery query,
        CancellationToken cancellationToken = default);
}
