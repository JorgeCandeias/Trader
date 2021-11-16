using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Algorithms;

internal class AlgoFactory<TAlgo> : IAlgoFactory
    where TAlgo : IAlgo
{
    private readonly IServiceProvider _provider;
    private readonly IAlgoContextFactory _contexts;

    public AlgoFactory(IServiceProvider provider, IAlgoContextFactory contexts)
    {
        _provider = provider;
        _contexts = contexts;
    }

    public IAlgo Create(string name)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));

        // create the scoped context
        var context = _contexts.Create(name);

        // set it as current in case the base algo requires it
        AlgoContext.Current = context;

        // resolve the algo instance now
        return ActivatorUtilities.CreateInstance<TAlgo>(_provider);
    }
}