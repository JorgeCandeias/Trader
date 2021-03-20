using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using Trader.Data;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqliteRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddSqliteRepository(this IServiceCollection services, Action<SqliteRepositoryOptions> configure)
        {
            return services
                .AddAutoMapper(options =>
                {
                    options.AddProfile<SqliteRepositoryProfile>();
                })
                .AddPooledDbContextFactory<TraderContext>((sp, options) =>
                {
                    var connectionString = sp.GetRequiredService<IOptions<SqliteRepositoryOptions>>().Value.ConnectionString;
                    options.UseSqlite(connectionString);
                })
                .AddSingleton<IOrdersRepository, SqliteOrdersRepository>()
                .AddOptions<SqliteRepositoryOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}