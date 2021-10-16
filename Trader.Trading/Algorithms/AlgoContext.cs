using Outcompute.Trader.Models;
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

        public Symbol Symbol { get; internal set; } = Symbol.Empty;

        public IServiceProvider ServiceProvider { get; }
    }
}