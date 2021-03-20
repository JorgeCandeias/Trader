using Microsoft.Extensions.DependencyInjection;
using System;

namespace Trader.Data
{
    public static class SqliteRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddSqliteRepository(this IServiceCollection services, Action<SqliteRepositoryOptions> configure)
        {
            return services
                .AddSingleton<ITradesRepository, SqliteTradesRepository>()
                .AddOptions<SqliteRepositoryOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}