using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context
{
    public interface IAlgoContext
    {
        /// <summary>
        /// The current algorithm name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The tick time of the last update.
        /// </summary>
        DateTime TickTime { get; }

        /// <summary>
        /// The service provider for extension methods to use.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Provides full symbol information For algos that derive from <see cref="ISymbolAlgo"/>.
        /// </summary>
        Symbol Symbol { get; }

        /// <summary>
        /// The default kline interval for the algo.
        /// </summary>
        KlineInterval KlineInterval { get; }

        /// <summary>
        /// The default kline interval periods for the algo.
        /// </summary>
        int KlinePeriods { get; }

        /// <summary>
        /// The current exchange information.
        /// </summary>
        ExchangeInfo ExchangeInfo { get; }

        /// <summary>
        /// The current significant asset information for the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        PositionDetails PositionDetails { get; }

        /// <summary>
        /// The current ticker for the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        MiniTicker Ticker { get; }

        /// <summary>
        /// The current spot balance for the base asset of the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        Balance BaseAssetSpotBalance { get; }

        /// <summary>
        /// The current spot balance for the quote asset of the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        Balance QuoteAssetSpotBalance { get; }

        /// <summary>
        /// The current savings balance for the base asset of the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        SavingsPosition BaseAssetSavingsBalance { get; }

        /// <summary>
        /// The current savings balance for the quote asset of the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        SavingsPosition QuoteAssetSavingsBalance { get; }

        /// <summary>
        /// The current swap pool balances for the base asset of the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        SwapPoolAssetBalance BaseAssetSwapPoolBalance { get; }

        /// <summary>
        /// The current swap pool balances for the quote asset of the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        SwapPoolAssetBalance QuoteAssetSwapPoolBalance { get; }

        /// <summary>
        /// Gets all historial orders for the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        IReadOnlyList<OrderQueryResult> Orders { get; }

        /// <summary>
        /// Gets all historial trades for the default symbol.
        /// This is only populated if the default symbol is defined.
        /// </summary>
        IReadOnlyList<AccountTrade> Trades { get; }

        /// <summary>
        /// Gets the klines for the default configuration.
        /// </summary>
        IReadOnlyList<Kline> Klines { get; }

        /// <summary>
        /// Gets the klines for all configured dependencies.
        /// </summary>
        IDictionary<(string Symbol, KlineInterval Interval), IReadOnlyList<Kline>> KlineLookup { get; }

        /// <summary>
        /// Makes the context self-update to the latest data.
        /// </summary>
        ValueTask UpdateAsync(CancellationToken cancellationToken = default);
    }
}