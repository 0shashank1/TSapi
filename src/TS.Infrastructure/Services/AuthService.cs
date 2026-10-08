using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TS.Application.Common;
using TS.Application.DTOs.Auth;
using TS.Application.DTOs.Users;
using TS.Application.Interfaces;
using TS.Domain.Entities;
using TS.Infrastructure.Persistence;
using TS.Infrastructure.Security;

namespace TS.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly TSDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        TSDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenGenerator refreshTokenGenerator,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenGenerator = refreshTokenGenerator;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var normalizedEmail = NormalizeEmail(email);

        var emailTaken = await _db.Users.AnyAsync(
            u => u.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (emailTaken)
        {
            throw new ConflictException(
                "An account with this email already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new User(
            email,
            normalizedEmail,
            _passwordHasher.Hash(request.Password),
            NormalizeDisplayName(request.DisplayName));

        var (refreshToken, plainRefreshToken) = CreateRefreshToken(
            user.Id, Guid.NewGuid(), now, ipAddress, userAgent);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        _db.Users.Add(user);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            UserSummary.From(user),
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            plainRefreshToken);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalizedEmail,
            cancellationToken);

        // Deliberately generic: never reveal whether the account exists.
        if (user is null ||
            !user.IsActive ||
            !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var now = DateTime.UtcNow;
        user.RecordLogin(now);

        var (refreshToken, plainRefreshToken) = CreateRefreshToken(
            user.Id, Guid.NewGuid(), now, ipAddress, userAgent);

        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            UserSummary.From(user),
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            plainRefreshToken);
    }

    public async Task<AuthResponse> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.Hash(refreshToken);

        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash,
                cancellationToken);

        if (token is null)
            throw new UnauthorizedException("Invalid refresh token.");

        var now = DateTime.UtcNow;

        if (token.IsRevoked)
        {
            // Reuse detection: a revoked token was presented again,
            // so the whole family is considered compromised.
            var family = await _db.RefreshTokens
                .Where(t => t.FamilyId == token.FamilyId &&
                            t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var member in family)
            {
                member.Revoke(now, "ReuseDetected", ipAddress);
            }

            await _db.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException(
                "Refresh token reuse detected. The session has been revoked.");
        }

        if (token.IsExpired(now))
            throw new UnauthorizedException("Refresh token has expired.");

        if (!token.User.IsActive)
            throw new UnauthorizedException("Account is not active.");

        var (replacement, plainRefreshToken) = CreateRefreshToken(
            token.UserId, token.FamilyId, now, ipAddress, userAgent);

        token.Revoke(now, "Rotated", ipAddress, replacement.Id);

        var accessToken = _jwtTokenService.GenerateAccessToken(token.User);

        _db.RefreshTokens.Add(replacement);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            UserSummary.From(token.User),
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            plainRefreshToken);
    }

    public async Task LogoutAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.Hash(refreshToken);

        var token = await _db.RefreshTokens.FirstOrDefaultAsync(
            t => t.TokenHash == tokenHash,
            cancellationToken);

        // Idempotent: revoking an unknown or already-revoked token
        // still results in 204.
        if (token is { IsRevoked: false })
        {
            token.Revoke(DateTime.UtcNow, "Logout", ipAddress);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task LogoutAllAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now, "LogoutAll", ipAddress);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private (RefreshToken Entity, string PlainToken) CreateRefreshToken(
        Guid userId,
        Guid familyId,
        DateTime now,
        string? ipAddress,
        string? userAgent)
    {
        var plainToken = _refreshTokenGenerator.Generate();

        var entity = new RefreshToken(
            userId,
            _refreshTokenGenerator.Hash(plainToken),
            familyId,
            now.AddDays(_jwtOptions.RefreshTokenDays),
            ipAddress,
            userAgent);

        return (entity, plainToken);
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToUpperInvariant();

    private static string? NormalizeDisplayName(string? displayName)
    {
        var trimmed = displayName?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
