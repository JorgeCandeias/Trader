using Microsoft.Extensions.DependencyInjection;
using System;

namespace Trader.Data.Sql
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
                .AddSingleton<SqlTraderRepository>()
                .AddOptions<SqlTraderRepositoryOptions>()
                .Configure(configure)
                .ValidateDataAnnotations()
                .Services;
        }
    }
}