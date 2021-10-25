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
            var context = _provider.GetRequiredService<AlgoContext>();
            context.Name = name;

            // resolve the algo instance now
            var algo = ActivatorUtilities.CreateInstance<TAlgo>(_provider);

            // set properties for derived algos
            if (algo is Algo derived)
            {
                derived.Context = context;
            }

            return algo;
        }
    }
}