using CTH.Database.Abstractions;
using CTH.Database.Infrastructure;
using CTH.Database.Repositories;
using CTH.Database.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CTH.Database.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddDatabaseInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

        services.TryAddSingleton<ISqlConnectionFactory, NpgsqlConnectionFactory>();
        services.TryAddSingleton<ISqlQueryProvider, SqlFileQueryProvider>();
        services.TryAddScoped<ISqlExecutor, SqlExecutor>();
        services.TryAddScoped<IUserAccountRepository, UserAccountRepository>();
        services.TryAddScoped<IUserSessionRepository, UserSessionRepository>();

        return services;
    }
}
