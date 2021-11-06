using Orleans.Runtime;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Configuration;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoServiceCollectionExtensions
    {
        public static IServiceCollection AddAlgo<TAlgo, TUserOptions>(this IServiceCollection services, string name, Action<AlgoOptions> configureAlgoOptions, Action<TUserOptions> configureUserOptions)
            where TAlgo : IAlgo
            where TUserOptions : class, new()
        {
            var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

            return services.AddAlgo(type, name, configureAlgoOptions, configureUserOptions);
        }

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

        public static IServiceCollection AddAlgos<TSource, TUserOptions>(
            this IServiceCollection services,
            IEnumerable<TSource> source,
            string type,
            Func<TSource, string> nameFactory,
            Action<TSource, AlgoOptions> configureAlgoOptions,
            Action<TSource, TUserOptions> configureUserOptions)
            where TUserOptions : class, new()
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (nameFactory is null) throw new ArgumentNullException(nameof(nameFactory));

            foreach (var item in source)
            {
                var name = nameFactory(item);

                services.AddAlgo<TUserOptions>(type, name, options => configureAlgoOptions(item, options), options => configureUserOptions(item, options));
            }

            return services;
        }

        internal static IServiceCollection AddAlgoTypeEntry<TAlgo>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
        {
            return services.AddSingletonNamedService<IAlgoTypeEntry>(typeName, (sp, k) => new AlgoTypeEntry(typeName, typeof(TAlgo)));
        }
    }
}