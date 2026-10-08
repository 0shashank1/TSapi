# Implement TextShare API controllers from api_design.md

No CQRS, no MediatR. Controllers call application services directly.

## Confirmed decisions
- **Service placement:** interfaces + DTOs in `TS.Application`, EF Core implementations in `TS.Infrastructure/Services`.
- **Scope:** everything — Problem Details errors, JWT bearer auth, refresh-token rotation w/ reuse detection, cursor pagination, HMAC share codes + short-lived share-access tokens, `If-Match` concurrency (409), rate limiting, OpenAPI + UI with bearer authorize, `/health` + `/health/ready`.
- **Migrations:** generate `InitialCreate` (dotnet-ef 10.0.12 installed; DB not needed for `migrations add`).
- **Config:** fix `appsettings.json` (Postgres connection string, Jwt section aligned to `JwtOptions`: `SecretKey`, `Issuer`, `Audience`, `AccessTokenMinutes`, `RefreshTokenDays`; dev secret in `appsettings.Development.json`), add `ShareCode:Keys` (versioned HMAC keys).

## Current state
Entities, EF configs, `TSDbContext`, `BCryptPasswordHasher`, `JwtTokenService`, `RefreshTokenGenerator` exist. Empty `Controllers/ Middleware/ Extensions/ Services/` in TS.Api. No DTOs, no services, no migrations, no auth configured. Build passes today.

## Layer responsibilities

```
Controller (TS.Api) → DTO validation + status codes + auth attributes
    → Application service interface (TS.Application: DTOs, exceptions, cursor helper)
        → Infrastructure service impl (TS.Infrastructure/Services: TSDbContext, domain)
```

Domain-failure mapping (via exceptions → Problem Details middleware):
`NotFoundException→404`, `ForbiddenException→403`, `UnauthorizedException→401`,
`ConflictException→409`, `ValidationException`/model-state→400 (+`errors` map), `ConcurrencyConflictException→409`.

---

## 1. TS.Domain — small domain additions
- `User`: `UpdateProfile(displayName, utcNow)`, `ChangePassword(newPasswordHash, utcNow)`, `SetRole(role, utcNow)`, `Activate(utcNow)` (keep existing `Deactivate`, `RecordLogin`).
- `ShareLink`: `UpdatePolicy(expiresAtUtc, maxUses, utcNow)`.
- `TextSnippet`: reuse `UpdateContent` (service merges PATCH nulls = keep existing).

## 2. TS.Application — new files
**Common**
- `Common/PagedResponse<T>` (`items`, `nextCursor`)
- `Common/Exceptions.cs`: `NotFoundException`, `ForbiddenException`, `UnauthorizedException`, `ConflictException`, `ConcurrencyConflictException`
- `Common/Cursor.cs`: base64url encode/decode of `(DateTime createdAtUtc, Guid id)` keyset cursor

**DTOs** (DataAnnotations for validation; null PATCH fields = unchanged)
- `Auth/`: `RegisterRequest`, `LoginRequest`, `RefreshRequest`, `LogoutRequest`, `AuthResponse` (`user`, `accessToken`, `accessTokenExpiresAtUtc`, `refreshToken`)
- `Users/`: `UserResponse`, `UserSummary`, `UpdateProfileRequest`, `ChangePasswordRequest`
- `Snippets/`: `CreateSnippetRequest`, `UpdateSnippetRequest`, `SnippetResponse`, `SnippetListItem` (no content), `SnippetListQuery` (pageSize, cursor, search, sortBy, sortDirection, status), `SnippetListResponse`
- `ShareLinks/`: `CreateShareLinkRequest` (expiresAtUtc, maxUses, password), `UpdateShareLinkRequest`, `ShareLinkResponse` (includes `url` only on create), `ShareLinkListItem`, `ShareLinkListResponse`
- `Public/`: `PublicShareResponse` (id, title, content, expiresAtUtc only), `UnlockRequest`, `UnlockResponse` (accessToken, expiresAtUtc)
- `Admin/`: `AdminUserListItem`, `AdminUserResponse` (+snippetCount, activeRefreshTokenCount), `AdminUserListQuery`, `AdminUserStatusRequest`, `AdminUserRoleRequest`, `AccessLogListItem`, `AccessLogQuery` (success, reason, from, to, cursor), `RefreshTokenListItem` (no token/tokenHash), `RefreshTokenQuery`

**Interfaces**
- `IAuthService`: `RegisterAsync`, `LoginAsync`, `RefreshAsync`, `LogoutAsync`, `LogoutAllAsync` (each takes ip/userAgent where relevant)
- `IUserService`: `GetMeAsync`, `UpdateProfileAsync`, `ChangePasswordAsync`, `DeactivateAsync`
- `ISnippetService`: `CreateAsync`, `ListAsync`, `GetAsync`, `UpdateAsync(id, dto, ifMatchVersion?)`, `DeleteAsync`
- `IShareLinkService`: `CreateAsync(snippetId, dto)`, `ListForSnippetAsync`, `GetAsync`, `UpdateAsync`, `RevokeAsync`
- `IPublicShareService`: `GetAsync(code, bearerToken?, ip, ua)`, `UnlockAsync(code, password, ip, ua)`
- `IAdminService`: `ListUsersAsync`, `GetUserAsync`, `SetStatusAsync`, `SetRoleAsync`, `ListAccessLogsAsync`, `ListRefreshTokensAsync`
- `IShareCodeService`: `GenerateCode()`, `HashCode(code, keyVersion)`, `CurrentKeyVersion`
- `IShareAccessTokenService`: `Generate(shareLinkId)`, `Validate(token)` → `Guid?`

## 3. TS.Infrastructure — new files
**Services** (all scoped, use `TSDbContext`, `TimeProvider`/`DateTime.UtcNow`, save changes):
- `Services/AuthService.cs`
  - Register: normalized-email uniqueness → `ConflictException`; hash password; issue refresh token (new family) + access token; `RecordLogin`.
  - Login: generic `UnauthorizedException` for bad creds/inactive; same token issuance.
  - Refresh: SHA-256 lookup → not found/expired → 401; **revoked → reuse detected → revoke whole family → 401**; else rotate (new token same `FamilyId`, old revoked with `ReplacedByTokenId`).
  - Logout: revoke by hash (idempotent, no throw). Logout-all: revoke all active for user.
- `Services/UserService.cs` — get/update profile; change password (verify → hash → revoke all RTs); deactivate (revoke all RTs).
- `Services/SnippetService.cs` — owner-scoped queries; create→201 data; list: keyset cursor + search + status(active/expired/all) + sort (createdAt default, title, viewCount, updatedAt), never returns full content; get: owner-or-admin else 404 (non-owner non-admin → 404/403 per matrix: not-owned → 403 for existing, missing → 404); update: `If-Match` version check → 409 + catch `DbUpdateConcurrencyException` → 409; delete → cascade share links.
- `Services/ShareLinkService.cs` — create link under snippet (owner check), generate code via `IShareCodeService`, store `HashCode(code, keyVersion)`, optional BCrypt password; response contains `url` (code) **only on create**; list/get/update policy/revoke (`Revoke` = set `RevokedAtUtc`, 204).
- `Services/PublicShareService.cs` — flow per doc §41: HMAC lookup → 404 (not found/revoked/expired/snippet expired/use-limit) → password-protected without valid share-access bearer → 401 `password-required` → atomic increment view + link use + access log (success) → 200 minimal payload. Every attempt writes `ShareAccessLog` (ip, user agent, failureReason). Unlock: verify BCrypt → share-access token; failures logged + `UnauthorizedException`.
- `Services/AdminService.cs` — paged user listing (search/status/role), user detail with counts, status change (deactivate → revoke RTs), role change (`ILogger` audit entry), access-log + refresh-token listings with filters/cursor; never expose passwordHash/token/tokenHash.

**Security**
- `Security/ShareCodeService.cs` — 16-char base62 random code via RNG, HMAC-SHA256 with key picked by version from `ShareCode:Keys` config.
- `Security/ShareAccessTokenService.cs` — JWT, purpose claim `share-access`, audience `TS.Share`, 10 min, validated by `Validate()`.

**Wire-up**
- `DependencyInjection.cs`: bind `JwtOptions`; `AddAuthentication` JWT bearer (validate issuer/audience/lifetime; map `sub`, `role` claims); `AddAuthorization` + `AdminOnly` policy (`role == admin`); register `JwtTokenService`, `RefreshTokenGenerator`, all services, `IShareCodeService`, `IShareAccessTokenService`; health check (`Database.CanConnectAsync`).

## 4. TS.Api — new files
- `Program.cs`: `AddControllers` + `ApiBehaviorOptions.InvalidModelStateResponseFactory` (RFC 9457, 400, `errors` map); `AddProblemDetails`; authN/authZ; rate limiter:
  - `auth` = fixed 10 req/min per IP → login/register/refresh
  - `unlock` = 5 req/min per IP+code → `POST /s/{code}/unlock`
  - `share` = 100 req/min per IP → `GET /s/{code}`
- `Middleware/ExceptionHandlingMiddleware.cs` — custom exceptions → Problem Details (`type` = `https://api.textshare.dev/problems/<kebab>`, title, status, detail, instance, traceId); `DbUpdateConcurrencyException` → 409; unhandled → 500.
- `Extensions/ClaimsPrincipalExtensions.cs` — `GetUserId()` (sub claim), `IsAdmin()`.
- `Extensions/RateLimitExtensions.cs` — policy registration.
- Controllers (`[ApiController]`, `[Produces("application/json")]`):
  - `Controllers/AuthController.cs` — `[Route("api/v1/auth")]`: POST register (201), login (200), refresh (200), logout (204, anonymous), logout-all (204, auth). Rate-limited `auth`.
  - `Controllers/UsersController.cs` — `[Route("api/v1/users")]`: GET/PATCH me (200), PATCH me/password (204), DELETE me (204).
  - `Controllers/SnippetsController.cs` — `[Route("api/v1/snippets")]`: POST (201 + `Location`), GET list (200, cursor), GET `{id}` (200), PATCH `{id}` (200, optional `If-Match` → 409), DELETE (204); nested POST/GET `{id}/links` (201/200).
  - `Controllers/ShareLinksController.cs` — `[Route("api/v1/share-links")]`: GET/PATCH/DELETE `{linkId}` (200/200/204).
  - `Controllers/PublicShareController.cs` — `[Route("s")]` anonymous: GET `{code}` (200 | 401 password-required | 404), POST `{code}/unlock` (200 | 401 | 429).
  - `Controllers/AdminUsersController.cs` — `[Route("api/v1/admin/users")]`, `[Authorize(Policy = "AdminOnly")]`: GET list, GET `{userId}`, PATCH `{userId}/status`, PATCH `{userId}/role`.
  - `Controllers/AdminAuditController.cs` — `[Route("api/v1/admin")]`, AdminOnly: GET `access-logs`, GET `refresh-tokens`.
  - `Controllers/HealthController.cs` — GET `/health` (liveness), GET `/health/ready` (DB check) — or `MapHealthChecks` equivalent.
- OpenAPI: `builder.Services.AddOpenApi()` + Swagger-compatible UI package (Scalar or Swashbuckle — pick whichever restores cleanly) with bearer authorize button.

## 5. Config + migration
- Rewrite `appsettings.json`: Postgres `Host=localhost;Database=ts;Username=postgres;Password=postgres`, Jwt keys matching `JwtOptions`, `ShareCode:Keys` (base64 key `"1"`), empty dev secret moved to `appsettings.Development.json`.
- `dotnet ef migrations add InitialCreate -p src/TS.Infrastructure -s src/TS.Api` (verify no design-time errors).

## Verification
1. `dotnet build TS.slnx` → 0 errors.
2. `dotnet test` → existing tests still pass.
3. `dotnet run --project src/TS.Api` briefly → `GET /health` returns 200 (no DB needed); `GET /openapi/v1.json` (or swagger) serves document. `/ready` will report unhealthy — expected, Postgres not running locally (docker daemon unavailable without sudo).
4. `dotnet ef migrations has-pending-model-changes` → false after migration.

## Out of scope
- Idempotency-Key support (doc marks it optional), DB-backed distributed rate limiting (in-memory limiter used), role-change/audit DB table (ILogger audit used), running actual Postgres.

## Status: COMPLETE

All sections implemented and verified against local Postgres:

- `dotnet build` → 0 errors / 0 warnings.
- `dotnet test` → 1 unit + 2 integration passing.
- `dotnet ef migrations has-pending-model-changes` → no pending changes.
- `/health` → 200; `/health/ready` → 200 (Healthy); `/swagger` + `/swagger/v1/swagger.json` → 200.
- End-to-end (`/tmp/opencode/e2e.py`, 70 checks) → **70 passed, 0 failed**.

Fixes applied during verification:
- `ShareAccessTokenService.Validate` — set `MapInboundClaims = false` so the `sub` claim is readable (share-access token was always rejected).
- `SnippetsController.Create` — return relative `Location` (manual header + `StatusCode(201, ...)`, not `CreatedAtAction`).
- `ShareLinkResponse.Url` — `[JsonIgnore(WhenWritingNull)]` so `url` is only serialized at creation.
- `ShareAccessLog.IpAddress` — `inet` → `varchar(45)`. Added `Microsoft.EntityFrameworkCore.Relational`, `Microsoft.EntityFrameworkCore.Design`, `Swashbuckle.AspNetCore` packages.
