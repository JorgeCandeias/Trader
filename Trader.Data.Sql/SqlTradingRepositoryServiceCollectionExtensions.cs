using System;
using Trader.Data;
using Trader.Data.Sql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqlTradingRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddSqlTradingRepository(this IServiceCollection services, Action<SqlTradingRepositoryOptions> configure)
        {
            return services
                .AddAutoMapper(options =>
                {
                    options.AddProfile<SqlTradingRepositoryProfile>();
                })
                .AddSingleton<ITradingRepository, SqlTradingRepository>()
                .AddOptions<SqlTradingRepositoryOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}