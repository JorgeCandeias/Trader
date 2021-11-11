using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoContextFactory : IAlgoContextFactory
    {
        private readonly IServiceProvider _provider;

        public AlgoContextFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IAlgoContext Create(string name)
        {
            return new AlgoContext(name, _provider);
        }
    }
}