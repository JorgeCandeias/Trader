using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using Outcompute.Trader.Trading.Algorithms.Positions;

namespace Outcompute.Trader.Trading.Algorithms.Context;

/// <summary>
/// Organizes multiple shards of data for a given symbol.
/// </summary>
[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "N/A")]
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

    public SymbolOrders Orders { get; } = new SymbolOrders();

    public TradeCollection Trades { get; set; } = TradeCollection.Empty;

    public KlineCollection Klines { get; set; } = KlineCollection.Empty;
}