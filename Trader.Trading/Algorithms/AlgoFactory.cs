using Microsoft.Extensions.DependencyInjection;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoFactory<TAlgo> : IAlgoFactory
        where TAlgo : IAlgo
    {
        private readonly IServiceProvider _provider;

        public AlgoFactory(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public IAlgo Create(string name)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));

            // set up the scoped context for the new algo to resolve
            AlgoContext.Current = _provider.GetRequiredService<AlgoContext>();
            AlgoContext.Current.Name = name;

            // resolve the algo instance now
            var algo = ActivatorUtilities.CreateInstance<TAlgo>(_provider);

            return algo;
        }
    }
}