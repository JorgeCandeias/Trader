using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoEntryServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgo<TUserOptions>(this IServiceCollection services, string type, string name, Action<AlgoOptions> configureAlgoOptions, Action<TUserOptions> configureUserOptions)
            where TUserOptions : class, new()
        {
            return services
                .AddSingleton<IAlgoEntry>(new AlgoEntry(name))
                .AddOptions<AlgoOptions>(name)
                    .Configure<IServiceProvider>((options, sp) =>
                    {
                        options.Type = type;
                    })
                    .Configure(configureAlgoOptions)
                    .ValidateDataAnnotations()
                    .Services
                .AddOptions<TUserOptions>(name)
                    .Configure(configureUserOptions)
                    .ValidateDataAnnotations()
                    .Services;
        }
    }
}