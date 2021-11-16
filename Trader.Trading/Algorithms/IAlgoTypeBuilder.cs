using Microsoft.Extensions.DependencyInjection;

namespace Outcompute.Trader.Trading.Algorithms;

public interface IAlgoTypeBuilder<TAlgo>
    where TAlgo : IAlgo
{
    public string TypeName { get; }

    public IServiceCollection Services { get; }

    IAlgoTypeBuilder<TAlgo, TOptions> AddOptionsType<TOptions>()
        where TOptions : class;

    IAlgoBuilder<TAlgo> AddAlgo(string name);

    IAlgoBuilder<TAlgo> AddAlgo(string name, string type);
}

public interface IAlgoTypeBuilder<TAlgo, TOptions> : IAlgoTypeBuilder<TAlgo>
    where TAlgo : IAlgo
    where TOptions : class
{
    new IAlgoBuilder<TAlgo, TOptions> AddAlgo(string name);

    new IAlgoBuilder<TAlgo, TOptions> AddAlgo(string name, string type);
}