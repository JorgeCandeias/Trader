using Microsoft.Extensions.DependencyInjection;

namespace Outcompute.Trader.Trading.Algorithms;

public interface IAlgoBuilder<TAlgo>
    where TAlgo : IAlgo
{
    public string Name { get; }

    public IServiceCollection Services { get; }

    IAlgoBuilder<TAlgo> ConfigureHostOptions(Action<AlgoOptions> configure);

    IAlgoBuilder<TAlgo, TOptions> ConfigureTypeOptions<TOptions>(Action<TOptions> configure)
        where TOptions : class;
}

public interface IAlgoBuilder<TAlgo, TOptions> : IAlgoBuilder<TAlgo>
    where TAlgo : IAlgo
    where TOptions : class
{
    new IAlgoBuilder<TAlgo, TOptions> ConfigureHostOptions(Action<AlgoOptions> configure);

    IAlgoBuilder<TAlgo, TOptions> ConfigureTypeOptions(Action<TOptions> configure);
}