using System;
using Trader.Data;
using Trader.Data.Sql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqlTraderRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddSqlTraderRepository(this IServiceCollection services, Action<SqlTraderRepositoryOptions> configure)
        {
            return services
                .AddAutoMapper(options =>
                {
                    options.AddProfile<SqlTraderRepositoryProfile>();
                })
                .AddSingleton<ITraderRepository, SqlTraderRepository>()
                .AddOptions<SqlTraderRepositoryOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}