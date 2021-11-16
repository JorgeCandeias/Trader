using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Runtime;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class AlgoServiceCollectionExtensions
{
    #region Shared

    private static void AddAlgoTypeCore<TAlgo>(IServiceCollection services, string type)
        where TAlgo : IAlgo
    {
        services
            .AddAlgoTypeEntry<TAlgo>(type)
            .AddTransientNamedService<IAlgoFactory, AlgoFactory<TAlgo>>(type);
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

    #region AddAlgoType

    /// <summary>
    /// Adds the specified algo type to the service provider and returns an <see cref="IAlgoTypeBuilder{TAlgo}"/> for further configuration.
    /// </summary>
    public static IAlgoTypeBuilder<TAlgo> AddAlgoType<TAlgo>(this IServiceCollection services)
        where TAlgo : IAlgo
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

        AddAlgoTypeCore<TAlgo>(services, type);

        return new AlgoTypeBuilder<TAlgo>(type, services);
    }

    /// <summary>
    /// Adds the specified algo type to the service provider and returns an <see cref="IAlgoTypeBuilder{TAlgo}"/> for further configuration.
    /// </summary>
    public static IAlgoTypeBuilder<TAlgo> AddAlgoType<TAlgo>(this IServiceCollection services, string type)
        where TAlgo : IAlgo
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (type is null) throw new ArgumentNullException(nameof(type));

        AddAlgoTypeCore<TAlgo>(services, type);

        return new AlgoTypeBuilder<TAlgo>(type, services);
    }

    #endregion AddAlgoType

    #region AddAlgo

    public static IAlgoBuilder<TAlgo> AddAlgo<TAlgo>(this IServiceCollection services, string name)
        where TAlgo : IAlgo
    {
        var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

        AddAlgoCore(services, name, type);

        return new AlgoBuilder<TAlgo>(name, services);
    }

    public static IAlgoBuilder<TAlgo, TOptions> AddAlgo<TAlgo, TOptions>(this IServiceCollection services, string name)
        where TAlgo : IAlgo
        where TOptions : class
    {
        var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

        AddAlgoCore(services, name, type);

        return new AlgoBuilder<TAlgo, TOptions>(name, services);
    }

    public static IAlgoBuilder<TAlgo, TOptions> AddAlgo<TAlgo, TOptions>(this IServiceCollection services, string name, string type)
        where TAlgo : IAlgo
        where TOptions : class
    {
        AddAlgoCore(services, name, type);

        return new AlgoBuilder<TAlgo, TOptions>(name, services);
    }

    #endregion AddAlgo
}