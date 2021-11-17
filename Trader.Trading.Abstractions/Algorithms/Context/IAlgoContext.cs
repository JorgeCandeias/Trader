﻿using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

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
    /// </summary>
    AutoPosition AutoPosition { get; }

    /// <summary>
    /// The current ticker for the default symbol.
    /// </summary>
    MiniTicker Ticker { get; }

    /// <summary>
    /// The current spot balance for the assets of the default symbol.
    /// </summary>
    SymbolSpotBalances Spot { get; }

    /// <summary>
    /// The current savings balances for the assets of the default symbol.
    /// </summary>
    SymbolSavingsBalances Savings { get; }

    /// <summary>
    /// The current swap pool balances for the assets of the default symbol.
    /// </summary>
    SymbolSwapPoolAssetBalances SwapPoolBalance { get; }

    /// <summary>
    /// Gets all historial orders for the default symbol.
    /// </summary>
    IReadOnlyList<OrderQueryResult> Orders { get; }

    /// <summary>
    /// Gets all historial trades for the default symbol.
    /// </summary>
    IReadOnlyList<AccountTrade> Trades { get; }

    /// <summary>
    /// Gets the klines for the default configuration.
    /// </summary>
    IReadOnlyList<Kline> Klines { get; }

    /// <summary>
    /// Gets context data for each declared symbol.
    /// </summary>
    SymbolDataCollection Data { get; }

    /// <summary>
    /// Makes the context self-update to the latest data.
    /// </summary>
    ValueTask UpdateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Organizes multiple shards of data for each symbol.
/// </summary>
public class SymbolDataCollection : KeyedCollection<string, SymbolData>
{
    protected override string GetKeyForItem(SymbolData item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));

        return item.Name;
    }

    public SymbolData GetOrAdd(string key)
    {
        if (TryGetValue(key, out var item))
        {
            return item;
        }

        item = new SymbolData(key);

        Add(item);

        return item;
    }
}

/// <summary>
/// Organizes multiple shards of data for a given symbol.
/// </summary>
public class SymbolData
{
    public SymbolData(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }

    public AutoPosition AutoPosition { get; set; } = AutoPosition.Empty;

    public MiniTicker Ticker { get; set; } = MiniTicker.Empty;

    public SymbolSpotBalances Spot { get; } = new SymbolSpotBalances();

    public SymbolSavingsBalances Savings { get; } = new SymbolSavingsBalances();

    public SymbolSwapPoolAssetBalances SwapPools { get; } = new SymbolSwapPoolAssetBalances();

    public IReadOnlyList<OrderQueryResult> Orders { get; set; } = ImmutableList<OrderQueryResult>.Empty;

    public IReadOnlyList<AccountTrade> Trades { get; set; } = ImmutableList<AccountTrade>.Empty;

    public IDictionary<KlineInterval, IReadOnlyList<Kline>> Klines { get; } = new Dictionary<KlineInterval, IReadOnlyList<Kline>>();
}

public class SymbolSpotBalances
{
    public Balance BaseAsset { get; set; } = Balance.Empty;

    public Balance QuoteAsset { get; set; } = Balance.Empty;
}

public class SymbolSavingsBalances
{
    public SavingsBalance BaseAsset { get; set; } = SavingsBalance.Empty;

    public SavingsBalance QuoteAsset { get; set; } = SavingsBalance.Empty;
}

public class SymbolSwapPoolAssetBalances
{
    public SwapPoolAssetBalance BaseAsset { get; set; } = SwapPoolAssetBalance.Empty;

    public SwapPoolAssetBalance QuoteAsset { get; set; } = SwapPoolAssetBalance.Empty;
}