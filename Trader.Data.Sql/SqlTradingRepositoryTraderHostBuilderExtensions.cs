using System;
using Outcompute.Trader.Data;
using Outcompute.Trader.Data.Sql;
using Outcompute.Trader.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqlTradingRepositoryTraderHostBuilderExtensions
    {
        public static ITraderHostBuilder AddSqlTradingRepository(this ITraderHostBuilder builder, Action<SqlTradingRepositoryOptions> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            builder.ConfigureServices((context, services) =>
            {
                services
                    .AddAutoMapper(options =>
                    {
                        options.AddProfile<SqlTradingRepositoryProfile>();
                    })
                    .AddSingleton<ITradingRepository, SqlTradingRepository>()
                    .AddOptions<SqlTradingRepositoryOptions>()
                    .Configure(configure)
                    .ValidateDataAnnotations();
            });

            return builder;
        }
    }
}