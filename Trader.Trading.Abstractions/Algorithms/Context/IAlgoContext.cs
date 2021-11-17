using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms.Context;

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
    /// Provides full symbol information for the default symbol declared by the algo.
    /// </summary>
    Symbol Symbol { get; }

    /// <summary>
    /// Provides full symbol information for the symbol list declared by the algo.
    /// </summary>
    IDictionary<string, Symbol> Symbols { get; }

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
    ExchangeInfo Exchange { get; }

    /// <summary>
    /// The current auto calculated positions for the default symbol.
    /// This is only populated if the default symbol is defined.
    /// </summary>
    AutoPosition AutoPosition => AutoPositions[Symbol.Name];

    /// <summary>
    /// The current auto calculated positions for all configured symbols.
    /// </summary>
    IDictionary<string, AutoPosition> AutoPositions { get; }

    /// <summary>
    /// The current ticker for the default symbol.
    /// This is only populated if the default symbol is defined.
    /// </summary>
    MiniTicker Ticker => Tickers[Symbol.Name];

    /// <summary>
    /// The current tickers for all configured symbols.
    /// </summary>
    IDictionary<string, MiniTicker> Tickers { get; }

    /// <summary>
    /// The current spot balance for the assets of the default symbol.
    /// </summary>
    SymbolSpotBalances SpotBalance => SpotBalances[Symbol.Name];

    /// <summary>
    /// The current spot balances for all referenced symbols.
    /// </summary>
    IDictionary<string, SymbolSpotBalances> SpotBalances { get; }

    /// <summary>
    /// The current savings balances for the assets of the default symbol.
    /// </summary>
    SymbolSavingsBalances SavingsBalances => Savings[Symbol.Name];

    /// <summary>
    /// Gets all savings balances for all configured symbols.
    /// </summary>
    IDictionary<string, SymbolSavingsBalances> Savings { get; }

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
    /// Gets all orders for all configured symbols and dependencies.
    /// </summary>
    IDictionary<string, IReadOnlyList<OrderQueryResult>> OrdersLookup { get; }

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
    /// Gets the klines for all configured symbols and dependencies.
    /// </summary>
    IDictionary<(string Symbol, KlineInterval Interval), IReadOnlyList<Kline>> KlineLookup { get; }

    /// <summary>
    /// Makes the context self-update to the latest data.
    /// </summary>
    ValueTask UpdateAsync(CancellationToken cancellationToken = default);
}

public record SymbolSavingsBalances(string Symbol, SavingsBalance BaseAsset, SavingsBalance QuoteAsset)
{
    public static SymbolSavingsBalances Empty { get; } = new SymbolSavingsBalances(string.Empty, SavingsBalance.Empty, SavingsBalance.Empty);
}

public record SymbolSpotBalances(string Symbol, Balance BaseAsset, Balance QuoteAsset)
{
    public static SymbolSpotBalances Empty { get; } = new SymbolSpotBalances(string.Empty, Balance.Empty, Balance.Empty);
}