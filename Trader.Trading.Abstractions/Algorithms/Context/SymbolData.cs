using Outcompute.Trader.Trading.Algorithms.Positions;

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

    private readonly List<Exception> _exceptions = new();
    private Symbol _symbol = Symbol.Empty;
    private AutoPosition _position = AutoPosition.Empty;
    private MiniTicker _ticker = MiniTicker.Empty;
    private readonly SymbolSpotBalances _spotBalances = new();
    private readonly SymbolSavingsBalances _savingsBalances = new();
    private readonly SymbolSwapPoolAssetBalances _swapBalances = new();
    private readonly SymbolOrders _orders = new();
    private ImmutableSortedSet<AccountTrade> _trades = ImmutableSortedSet<AccountTrade>.Empty;
    private ImmutableSortedSet<Kline> _klines = ImmutableSortedSet<Kline>.Empty;

    private IDictionary<(string Symbol, KlineInterval Interval), ImmutableSortedSet<Kline>> _klineDependencies = new Dictionary<(string Symbol, KlineInterval Interval), ImmutableSortedSet<Kline>>();

    /// <summary>
    /// The name of the symbol.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Returns <see cref="false"/> if there were any problems populating the symbol data.
    /// Otherwise returns <see cref="true"/>.
    /// Examine the <see cref="Exceptions"/> property for more information.
    /// </summary>
    public bool IsValid => _exceptions.Count == 0;

    /// <summary>
    /// Returns any exceptions generated while populating the symbol data.
    /// </summary>
    public IList<Exception> Exceptions => _exceptions;

    /// <summary>
    /// The exchange information for the symbol.
    /// </summary>
    public Symbol Symbol
    {
        get
        {
            ThrowOnInvalid();
            return _symbol;
        }
        set
        {
            _symbol = value;
        }
    }

    /// <summary>
    /// The automatically resolved positions for the symbol based on exchange order and trade history.
    /// </summary>
    public AutoPosition AutoPosition
    {
        get
        {
            ThrowOnInvalid();
            return _position;
        }
        set
        {
            _position = value;
        }
    }

    /// <summary>
    /// The current ticker information for the symbol.
    /// </summary>
    public MiniTicker Ticker
    {
        get
        {
            ThrowOnInvalid();
            return _ticker;
        }
        set
        {
            _ticker = value;
        }
    }

    /// <summary>
    /// The current spot balances for the assets of the symbol.
    /// </summary>
    public SymbolSpotBalances Spot
    {
        get
        {
            ThrowOnInvalid();
            return _spotBalances;
        }
    }

    /// <summary>
    /// The current savings balances for the assets of the symbol.
    /// </summary>
    public SymbolSavingsBalances Savings
    {
        get
        {
            ThrowOnInvalid();
            return _savingsBalances;
        }
    }

    /// <summary>
    /// The current swap pool balances for the assets of the symbol.
    /// </summary>
    public SymbolSwapPoolAssetBalances SwapPools
    {
        get
        {
            ThrowOnInvalid();
            return _swapBalances;
        }
    }

    /// <summary>
    /// The order history of the current symbol.
    /// </summary>
    public SymbolOrders Orders
    {
        get
        {
            ThrowOnInvalid();
            return _orders;
        }
    }

    /// <summary>
    /// The trade history of the current symbol.
    /// </summary>
    public ImmutableSortedSet<AccountTrade> Trades
    {
        get
        {
            ThrowOnInvalid();
            return _trades;
        }
        set
        {
            _trades = value;
        }
    }

    /// <summary>
    /// The default kline history of the current symbol.
    /// </summary>
    public ImmutableSortedSet<Kline> Klines
    {
        get
        {
            ThrowOnInvalid();
            return _klines;
        }
        set
        {
            _klines = value;
        }
    }

    public IDictionary<(string Symbol, KlineInterval Interval), ImmutableSortedSet<Kline>> KlineDependencies
    {
        get
        {
            ThrowOnInvalid();
            return _klineDependencies;
        }
    }

    public void ThrowOnInvalid()
    {
        if (_exceptions.Count > 0)
        {
            throw new AggregateException(_exceptions);
        }
    }
}