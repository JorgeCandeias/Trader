using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoContext : IAlgoContext
    {
        public AlgoContext(IServiceProvider serviceProvider)
        {
            Name = AlgoFactoryContext.AlgoName;
            ServiceProvider = serviceProvider;
        }

        public string Name { get; }

        public IServiceProvider ServiceProvider { get; }
    }
}