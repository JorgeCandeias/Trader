using Microsoft.Extensions.DependencyInjection;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoFactory<TAlgo> : IAlgoFactory
        where TAlgo : IAlgo
    {
        private readonly IServiceProvider _provider;
        private readonly ObjectFactory _factory;

        public AlgoFactory(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _factory = ActivatorUtilities.CreateFactory(typeof(TAlgo), Array.Empty<Type>());
        }

        public IAlgo Create(string name)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));

            AlgoFactoryContext.AlgoName = name;

            return (IAlgo)_factory(_provider, Array.Empty<object>());
        }
    }
}