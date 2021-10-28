using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoContext
    {
        /// <summary>
        /// The current algorithm name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Provides full symbol information For algos that derive from <see cref="ISymbolAlgo"/>.
        /// </summary>
        Symbol Symbol { get; }

        /// <summary>
        /// The service provider for extension methods to use.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// The current significant asset information for the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        SignificantResult Significant { get; }

        /// <summary>
        /// The current ticker for the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        MiniTicker Ticker { get; }

        /// <summary>
        /// The current spot balance for the base asset of the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        Balance AssetBalance { get; }

        /// <summary>
        /// The current spot balance for the quote asset of the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        Balance QuoteBalance { get; }
    }
}