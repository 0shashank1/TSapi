using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TS.Application.Interfaces;
using TS.Infrastructure.Persistence;
using TS.Infrastructure.Persistence.Repositories;
using TS.Infrastructure.Security;
using TS.Infrastructure.Services;

namespace TS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TSDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.Configure<ShareCodeOptions>(
            configuration.GetSection(ShareCodeOptions.SectionName));

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IShareCodeService, ShareCodeService>();
        services.AddScoped<IShareAccessTokenService, ShareAccessTokenService>();

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISnippetRepository, SnippetRepository>();
        services.AddScoped<IShareLinkRepository, ShareLinkRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IShareAccessLogRepository, ShareAccessLogRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISnippetService, SnippetService>();
        services.AddScoped<IShareLinkService, ShareLinkService>();
        services.AddScoped<IPublicShareService, PublicShareService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
