using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Runtime;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Configuration;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the specified algo type to the service provider and returns an <see cref="IAlgoTypeBuilder{TAlgo}"/> for further configuration.
        /// </summary>
        public static IAlgoTypeBuilder AddAlgoType<TAlgo>(this IServiceCollection services)
            where TAlgo : IAlgo
        {
            var typeName = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

            return services.AddAlgoType<TAlgo>(typeName);
        }

        /// <summary>
        /// Adds the specified algo type to the service provider and returns an <see cref="IAlgoTypeBuilder{TAlgo}"/> for further configuration.
        /// </summary>
        public static IAlgoTypeBuilder AddAlgoType<TAlgo>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
        {
            services
                .AddAlgoTypeEntry<TAlgo>(typeName)
                .AddSingletonNamedService<IAlgoFactory, AlgoFactory<TAlgo>>(typeName);

            return new AlgoTypeBuilder(typeName, services);
        }

        /// <summary>
        /// Configures automatic named configuration for the specified options type.
        /// </summary>
        public static IAlgoTypeBuilder AddOptionsType<TOptions>(this IAlgoTypeBuilder builder)
            where TOptions : class
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            builder.Services.ConfigureOptions<AlgoUserOptionsConfigurator<TOptions>>();

            return builder;
        }

        public static IAlgoBuilder AddAlgo<TAlgo>(this IAlgoTypeBuilder builder, string name)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            return builder.Services.AddAlgo<TAlgo>(name);
        }

        public static IAlgoBuilder AddAlgo<TAlgo>(this IServiceCollection services, string name)
        {
            var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

            services
                .AddSingleton<IAlgoEntry>(new AlgoEntry(name))
                .AddOptions<AlgoOptions>(name)
                .Configure(options =>
                {
                    options.Type = type;
                })
                .ValidateDataAnnotations();

            return new AlgoBuilder(name, services);
        }

        public static IAlgoBuilder AddAlgo(this IAlgoTypeBuilder builder, string name, string type)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            return builder.AddAlgo(name, type);
        }

        public static IAlgoBuilder AddAlgo(this IServiceCollection services, string name, string type)
        {
            services
                .AddSingleton<IAlgoEntry>(new AlgoEntry(name))
                .AddOptions<AlgoOptions>(name)
                .Configure(options =>
                {
                    options.Type = type;
                })
                .ValidateDataAnnotations();

            return new AlgoBuilder(name, services);
        }

        public static IAlgoBuilder ConfigureHostOptions(this IAlgoBuilder builder, Action<AlgoOptions> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            builder.Services
                .Configure(builder.Name, configure);

            return builder;
        }

        public static IAlgoBuilder ConfigureTypeOptions<TOptions>(this IAlgoBuilder builder, Action<TOptions> configure)
            where TOptions : class
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            builder.Services
                .AddOptions<TOptions>(builder.Name)
                .Configure(configure)
                .ValidateDataAnnotations();

            return builder;
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

                services
                    .AddAlgo(name, type)
                    .ConfigureHostOptions(options => configureAlgoOptions(item, options))
                    .ConfigureTypeOptions<TUserOptions>(options => configureUserOptions(item, options));
            }

            return services;
        }

        internal static IServiceCollection AddAlgoTypeEntry<TAlgo>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
        {
            return services.AddSingletonNamedService<IAlgoTypeEntry>(typeName, (sp, k) => new AlgoTypeEntry(typeName, typeof(TAlgo)));
        }

        public static IServiceCollection TryAddKeyedServiceCollection(this IServiceCollection services)
        {
            services.TryAddSingleton(typeof(IKeyedServiceCollection<,>), typeof(KeyedServiceCollection<,>));

            return services;
        }
    }
}