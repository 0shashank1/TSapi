using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TS.Api.Extensions;
using TS.Infrastructure.Security;

namespace TS.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var secretKey = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Convert.FromBase64String(secretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        if (!context.Response.HasStarted)
                        {
                            await ApiProblems.WriteAsync(
                                context.HttpContext,
                                StatusCodes.Status401Unauthorized,
                                "unauthorized",
                                "Unauthorized",
                                "Authentication is required or the access token is invalid.");
                        }
                    },
                    OnForbidden = async context =>
                    {
                        if (!context.Response.HasStarted)
                        {
                            await ApiProblems.WriteAsync(
                                context.HttpContext,
                                StatusCodes.Status403Forbidden,
                                "access-denied",
                                "Access denied",
                                "You do not have permission to perform this action.");
                        }
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                "AdminOnly",
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim("role", "admin"));
        });

        return services;
    }
}
