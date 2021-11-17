using Outcompute.Trader.Models;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Algorithms.Context;

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

    public Symbol Symbol { get; set; } = Symbol.Empty;

    public AutoPosition AutoPosition { get; set; } = AutoPosition.Empty;

    public MiniTicker Ticker { get; set; } = MiniTicker.Empty;

    public SymbolSpotBalances Spot { get; } = new SymbolSpotBalances();

    public SymbolSavingsBalances Savings { get; } = new SymbolSavingsBalances();

    public SymbolSwapPoolAssetBalances SwapPools { get; } = new SymbolSwapPoolAssetBalances();

    public IReadOnlyList<OrderQueryResult> Orders { get; set; } = ImmutableList<OrderQueryResult>.Empty;

    public IReadOnlyList<AccountTrade> Trades { get; set; } = ImmutableList<AccountTrade>.Empty;

    public IReadOnlyList<Kline> Klines { get; set; } = ImmutableList<Kline>.Empty;
}