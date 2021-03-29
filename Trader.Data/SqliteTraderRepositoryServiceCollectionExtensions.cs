using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Trader.Data;
using Trader.Data.Converters;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqliteTraderRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddSqliteRepository(this IServiceCollection services, Action<SqliteTraderRepositoryOptions> configure)
        {
            return services
                .AddAutoMapper(options =>
                {
                    options.AddProfile<SqliteTraderRepositoryProfile>();
                })
                .AddPooledDbContextFactory<TraderContext>((sp, options) =>
                {
                    var connectionString = sp.GetRequiredService<IOptions<SqliteTraderRepositoryOptions>>().Value.ConnectionString;
                    var logger = sp.GetRequiredService<ILogger<SqliteTraderRepository>>();

                    logger.LogInformation("{Name} using connection string {ConnectionString}", nameof(SqliteTraderRepository), connectionString);

                    options.UseSqlite(connectionString);
                })
                .AddSingleton<OrderGroupConverter>()
                .AddSingleton<SqliteTraderRepository>()
                .AddSingleton<ITraderRepository>(sp => sp.GetRequiredService<SqliteTraderRepository>())
                .AddSingleton<IHostedService>(sp => sp.GetRequiredService<SqliteTraderRepository>())
                .AddOptions<SqliteTraderRepositoryOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}