using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Runtime;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AlgoServiceCollectionExtensions
    {
        #region Shared

        private static void AddAlgoTypeCore<TAlgo>(IServiceCollection services, string type)
            where TAlgo : IAlgo
        {
            services
                .AddAlgoTypeEntry<TAlgo>(type)
                .AddSingletonNamedService<IAlgoFactory, AlgoFactory<TAlgo>>(type);
        }

        private static void AddAlgoCore(IServiceCollection services, string name, string type)
        {
            services
                .AddSingleton<IAlgoEntry>(new AlgoEntry(name))
                .AddOptions<AlgoOptions>(name)
                .Configure(options =>
                {
                    options.Type = type;
                })
                .ValidateDataAnnotations();
        }

        private static void ConfigureTypeOptionsCore<TOptions>(IServiceCollection services, string name, Action<TOptions> configure)
            where TOptions : class
        {
            services
                .AddOptions<TOptions>(name)
                .Configure(configure)
                .ValidateDataAnnotations();
        }

        private static IServiceCollection AddAlgoTypeEntry<TAlgo>(this IServiceCollection services, string typeName)
            where TAlgo : IAlgo
        {
            return services.AddSingletonNamedService<IAlgoTypeEntry>(typeName, (sp, k) => new AlgoTypeEntry(typeName, typeof(TAlgo)));
        }

        internal static IServiceCollection TryAddKeyedServiceCollection(this IServiceCollection services)
        {
            services.TryAddSingleton(typeof(IKeyedServiceCollection<,>), typeof(KeyedServiceCollection<,>));

            return services;
        }

        #endregion Shared

        #region IServiceCollection.AddAlgoType

        /// <summary>
        /// Adds the specified algo type to the service provider and returns an <see cref="IAlgoTypeBuilder{TAlgo}"/> for further configuration.
        /// </summary>
        public static IAlgoTypeBuilder AddAlgoType<TAlgo>(this IServiceCollection services)
            where TAlgo : IAlgo
        {
            if (services is null) throw new ArgumentNullException(nameof(services));

            var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

            AddAlgoTypeCore<TAlgo>(services, type);

            return new AlgoTypeBuilder(type, services);
        }

        /// <summary>
        /// Adds the specified algo type to the service provider and returns an <see cref="IAlgoTypeBuilder{TAlgo}"/> for further configuration.
        /// </summary>
        public static IAlgoTypeBuilder AddAlgoType<TAlgo>(this IServiceCollection services, string type)
            where TAlgo : IAlgo
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (type is null) throw new ArgumentNullException(nameof(type));

            AddAlgoTypeCore<TAlgo>(services, type);

            return new AlgoTypeBuilder(type, services);
        }

        #endregion IServiceCollection.AddAlgoType

        #region IAlgoTypeBuilder.AddOptionsType

        /// <summary>
        /// Configures automatic named configuration for the specified options type.
        /// </summary>
        public static IAlgoTypeBuilder<TOptions> AddOptionsType<TOptions>(this IAlgoTypeBuilder builder)
            where TOptions : class
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            builder.Services.ConfigureOptions<AlgoUserOptionsConfigurator<TOptions>>();

            return new AlgoTypeBuilder<TOptions>(builder.TypeName, builder.Services);
        }

        #endregion IAlgoTypeBuilder.AddOptionsType

        #region IServiceCollection.AddAlgo

        public static IAlgoBuilder AddAlgo<TAlgo>(this IServiceCollection services, string name)
        {
            var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

            AddAlgoCore(services, name, type);

            return new AlgoBuilder(name, services);
        }

        public static IAlgoBuilder<TOptions> AddAlgo<TAlgo, TOptions>(this IServiceCollection services, string name)
        {
            var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

            AddAlgoCore(services, name, type);

            return new AlgoBuilder<TOptions>(name, services);
        }

        public static IAlgoBuilder AddAlgo(this IServiceCollection services, string name, string type)
        {
            AddAlgoCore(services, name, type);

            return new AlgoBuilder(name, services);
        }

        public static IAlgoBuilder<TOptions> AddAlgo<TOptions>(this IServiceCollection services, string name, string type)
        {
            AddAlgoCore(services, name, type);

            return new AlgoBuilder<TOptions>(name, services);
        }

        #endregion IServiceCollection.AddAlgo

        #region IAlgoTypeBuilder.AddAlgo

        public static IAlgoBuilder AddAlgo(this IAlgoTypeBuilder builder, string name, string type)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            AddAlgoCore(builder.Services, name, type);

            return new AlgoBuilder(name, builder.Services);
        }

        public static IAlgoBuilder AddAlgo<TAlgo>(this IAlgoTypeBuilder builder, string name)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

            AddAlgoCore(builder.Services, name, type);

            return new AlgoBuilder(name, builder.Services);
        }

        public static IAlgoBuilder<TOptions> AddAlgo<TOptions>(this IAlgoTypeBuilder<TOptions> builder, string name, string type)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            AddAlgoCore(builder.Services, name, type);

            return new AlgoBuilder<TOptions>(name, builder.Services);
        }

        public static IAlgoBuilder<TOptions> AddAlgo<TAlgo, TOptions>(this IAlgoTypeBuilder<TOptions> builder, string name)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

            AddAlgoCore(builder.Services, name, type);

            return new AlgoBuilder<TOptions>(name, builder.Services);
        }

        #endregion IAlgoTypeBuilder.AddAlgo

        #region IAlgoBuilder.ConfigureHostOptions

        public static IAlgoBuilder ConfigureHostOptions(this IAlgoBuilder builder, Action<AlgoOptions> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            builder.Services
                .Configure(builder.Name, configure);

            return builder;
        }

        public static IAlgoBuilder<TOptions> ConfigureHostOptions<TOptions>(this IAlgoBuilder<TOptions> builder, Action<AlgoOptions> configure)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            builder.Services
                .Configure(builder.Name, configure);

            return builder;
        }

        #endregion IAlgoBuilder.ConfigureHostOptions

        #region ConfigureTypeOptions

        public static IAlgoBuilder ConfigureTypeOptions<TOptions>(this IAlgoBuilder builder, Action<TOptions> configure)
            where TOptions : class
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            ConfigureTypeOptionsCore(builder.Services, builder.Name, configure);

            return builder;
        }

        public static IAlgoBuilder<TOptions> ConfigureTypeOptions<TOptions>(this IAlgoBuilder<TOptions> builder, Action<TOptions> configure)
            where TOptions : class
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            ConfigureTypeOptionsCore(builder.Services, builder.Name, configure);

            return builder;
        }

        #endregion ConfigureTypeOptions
    }
}