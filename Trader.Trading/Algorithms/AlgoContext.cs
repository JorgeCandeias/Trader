using System;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoContext : IAlgoContext
    {
        public AlgoContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public string Name { get; internal set; } = Empty;

        public IServiceProvider ServiceProvider { get; }
    }
}