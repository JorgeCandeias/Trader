using Microsoft.Extensions.DependencyInjection;

namespace Outcompute.Trader.Trading.Algorithms.Context;

internal class AlgoContextFactory : IAlgoContextFactory
{
    private readonly IServiceProvider _provider;

    public AlgoContextFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IAlgoContext Create(string name)
    {
        var context = new AlgoContext(name, _provider);

        _provider.GetRequiredService<IAlgoContextLocal>().Context = context;

        return context;
    }
}