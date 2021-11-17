using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Immutable;

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

    public IDictionary<string, Symbol> Symbols { get; } = new Dictionary<string, Symbol>();

    public KlineInterval KlineInterval { get; set; } = KlineInterval.None;

    public int KlinePeriods { get; set; }

    public ExchangeInfo ExchangeInfo { get; set; } = ExchangeInfo.Empty;

    public PositionDetails PositionDetails => PositionDetailsLookup[Symbol.Name];

    public IDictionary<string, PositionDetails> PositionDetailsLookup { get; } = new Dictionary<string, PositionDetails>();

    public MiniTicker Ticker => Tickers[Symbol.Name];

    public IDictionary<string, MiniTicker> Tickers { get; } = new Dictionary<string, MiniTicker>();

    public IDictionary<string, SymbolSpotBalances> SpotBalances { get; } = new Dictionary<string, SymbolSpotBalances>();

    public IDictionary<string, SymbolSavingsPositions> SavingsLookup { get; set; } = new Dictionary<string, SymbolSavingsPositions>();

    public SwapPoolAssetBalance BaseAssetSwapPoolBalance { get; set; } = SwapPoolAssetBalance.Empty;

    public SwapPoolAssetBalance QuoteAssetSwapPoolBalance { get; set; } = SwapPoolAssetBalance.Empty;

    public IReadOnlyList<OrderQueryResult> Orders => OrdersLookup[Symbol.Name];

    public IDictionary<string, IReadOnlyList<OrderQueryResult>> OrdersLookup { get; } = new Dictionary<string, IReadOnlyList<OrderQueryResult>>();

    public IReadOnlyList<AccountTrade> Trades { get; set; } = ImmutableList<AccountTrade>.Empty;

    public IReadOnlyList<Kline> Klines => KlineLookup[(Symbol.Name, KlineInterval)];

    public IDictionary<(string Symbol, KlineInterval Interval), IReadOnlyList<Kline>> KlineLookup { get; } = new Dictionary<(string Symbol, KlineInterval Interval), IReadOnlyList<Kline>>();

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