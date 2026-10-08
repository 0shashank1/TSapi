using System.Security.Claims;
using TS.Application.Common;

namespace TS.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("sub")
                    ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (value is null || !Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedException(
                "The access token does not contain a valid subject.");
        }

        return userId;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
        => principal.HasClaim("role", "admin") ||
           principal.IsInRole("admin");

    public static AccessContext ToAccessContext(this ClaimsPrincipal principal)
        => new(principal.GetUserId(), principal.IsAdmin());
}
