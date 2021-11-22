using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Configuration;

namespace Outcompute.Trader.Trading.Algorithms;

internal class AlgoTypeBuilder<TAlgo> : IAlgoTypeBuilder<TAlgo>
    where TAlgo : IAlgo
{
    public AlgoTypeBuilder(string typeName, IServiceCollection services)
    {
        TypeName = typeName;
        Services = services;
    }

    public string TypeName { get; }

    public IServiceCollection Services { get; }

    protected void AddAlgoCore(string name, string type)
    {
        Services
            .AddSingleton<IAlgoEntry>(new AlgoEntry(name))
            .AddOptions<AlgoOptions>(name)
            .Configure(options =>
            {
                options.Type = type;
            })
            .ValidateDataAnnotations();
    }

    public IAlgoTypeBuilder<TAlgo, TOptions> AddOptionsType<TOptions>()
        where TOptions : class
    {
        Services.AddOptions<TOptions>().ValidateDataAnnotations();
        Services.ConfigureOptions<AlgoUserOptionsConfigurator<TOptions>>();

        return new AlgoTypeBuilder<TAlgo, TOptions>(TypeName, Services);
    }

    public IAlgoBuilder<TAlgo> AddAlgo(string name)
    {
        var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

        AddAlgoCore(name, type);

        return new AlgoBuilder<TAlgo>(name, Services);
    }

    public IAlgoBuilder<TAlgo> AddAlgo(string name, string type)
    {
        AddAlgoCore(name, type);

        return new AlgoBuilder<TAlgo>(name, Services);
    }
}

internal class AlgoTypeBuilder<TAlgo, TOptions> : AlgoTypeBuilder<TAlgo>, IAlgoTypeBuilder<TAlgo, TOptions>
    where TAlgo : IAlgo
    where TOptions : class
{
    public AlgoTypeBuilder(string typeName, IServiceCollection services)
        : base(typeName, services)
    {
    }

    public new IAlgoBuilder<TAlgo, TOptions> AddAlgo(string name, string type)
    {
        AddAlgoCore(name, type);

        return new AlgoBuilder<TAlgo, TOptions>(name, Services);
    }

    public new IAlgoBuilder<TAlgo, TOptions> AddAlgo(string name)
    {
        var type = typeof(TAlgo).AssemblyQualifiedName ?? throw new InvalidOperationException();

        AddAlgoCore(name, type);

        return new AlgoBuilder<TAlgo, TOptions>(name, Services);
    }
}