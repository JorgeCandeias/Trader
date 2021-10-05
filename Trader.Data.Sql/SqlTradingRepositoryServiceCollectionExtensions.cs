using Outcompute.Trader.Data;
using Outcompute.Trader.Data.Sql;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqlTradingRepositoryServiceCollectionExtensions
    {
        public static IServiceCollection AddSqlTradingRepository(this IServiceCollection services, Action<SqlTradingRepositoryOptions> configure)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

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