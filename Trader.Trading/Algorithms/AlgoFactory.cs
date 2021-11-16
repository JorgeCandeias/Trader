using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Algorithms;

internal class AlgoFactory<TAlgo> : IAlgoFactory
    where TAlgo : IAlgo
{
    private readonly IAlgoContextFactory _factory;
    private readonly IServiceProvider _provider;

    public AlgoFactory(IAlgoContextFactory factory, IServiceProvider provider)
    {
        _factory = factory;
        _provider = provider;
    }

    public IAlgo Create(string name)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));

        // create a new context
        var context = _factory.Create(name);

        // set it as current in case the algo wants to require it
        AlgoContext.Current = context;

        // resolve the algo instance now
        return ActivatorUtilities.CreateInstance<TAlgo>(_provider);
    }
}