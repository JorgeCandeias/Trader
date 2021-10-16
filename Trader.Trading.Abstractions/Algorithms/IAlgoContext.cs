using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoContext
    {
        /// <summary>
        /// The current algorithm name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Provides full symbol information For algos that derive from <see cref="ISymbolAlgo"/>.
        /// </summary>
        Symbol Symbol { get; }

        /// <summary>
        /// The service provider for extension methods to use.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }
    }
}