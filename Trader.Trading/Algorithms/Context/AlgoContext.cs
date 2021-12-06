using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Core;
using Outcompute.Trader.Trading.Algorithms.Positions;

namespace Outcompute.Trader.Trading.Algorithms.Context;

internal class AlgoContext : IAlgoContext
{
    private readonly IEnumerable<IAlgoContextConfigurator<AlgoContext>> _configurators;

    public AlgoContext(string name, IServiceProvider serviceProvider)
    {
        Name = name;
        ServiceProvider = serviceProvider;

        _configurators = ServiceProvider.GetRequiredService<IEnumerable<IAlgoContextConfigurator<AlgoContext>>>();
    }

    public string Name { get; }

    public DateTime TickTime { get; set; }

    public IServiceProvider ServiceProvider { get; }

    public Symbol Symbol { get; set; } = Symbol.Empty;

    public SymbolSet Symbols { get; } = new SymbolSet();

    public KlineInterval KlineInterval { get; set; } = KlineInterval.None;

    public int KlinePeriods { get; set; }

    public ExchangeInfo Exchange { get; set; } = ExchangeInfo.Empty;

    public AutoPosition AutoPosition => Data[Symbol.Name].AutoPosition;

    public MiniTicker Ticker => Data[Symbol.Name].Ticker;

    public SymbolSpotBalances Spot => Data[Symbol.Name].Spot;

    public SymbolSavingsBalances Savings => Data[Symbol.Name].Savings;

    public SymbolSwapPoolAssetBalances SwapPoolBalance => Data[Symbol.Name].SwapPools;

    public SymbolOrders Orders => Data[Symbol.Name].Orders;

    public ImmutableSortedSet<AccountTrade> Trades => Data[Symbol.Name].Trades;

    public KlineCollection Klines => Data[Symbol.Name].Klines;

    public SymbolDataCollection Data { get; } = new SymbolDataCollection();

    public async ValueTask UpdateAsync(CancellationToken cancellationToken = default)
    {
        foreach (var configurator in _configurators)
        {
            await configurator.ConfigureAsync(this, Name, cancellationToken).ConfigureAwait(false);
        }
    }

    #region Static Helpers

    public static AlgoContext Empty { get; } = new AlgoContext(string.Empty, NullServiceProvider.Instance);

    private static readonly AsyncLocal<IAlgoContext> _current = new();

    internal static IAlgoContext Current
    {
        get
        {
            return _current.Value ?? Empty;
        }
        set
        {
            _current.Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    #endregion Static Helpers
}