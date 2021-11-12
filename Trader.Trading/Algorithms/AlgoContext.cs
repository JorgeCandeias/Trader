using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System.Collections.Immutable;

namespace Outcompute.Trader.Trading.Algorithms
{
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

        public KlineInterval KlineInterval { get; set; } = KlineInterval.None;

        public int KlinePeriods { get; set; }

        public PositionDetails PositionDetails { get; set; } = PositionDetails.Empty;

        public MiniTicker Ticker { get; set; } = MiniTicker.Empty;

        public Balance AssetSpotBalance { get; set; } = Balance.Empty;

        public Balance QuoteSpotBalance { get; set; } = Balance.Empty;

        public SavingsPosition AssetSavingsBalance { get; set; } = SavingsPosition.Empty;

        public SavingsPosition QuoteSavingsBalance { get; set; } = SavingsPosition.Empty;

        public SwapPoolAssetBalance AssetSwapPoolBalance { get; set; } = SwapPoolAssetBalance.Empty;

        public SwapPoolAssetBalance QuoteSwapPoolBalance { get; set; } = SwapPoolAssetBalance.Empty;

        public IReadOnlyList<OrderQueryResult> Orders { get; set; } = ImmutableList<OrderQueryResult>.Empty;

        public IDictionary<(string Symbol, KlineInterval Interval), IReadOnlyCollection<Kline>> KlineLookup { get; } = new Dictionary<(string Symbol, KlineInterval Interval), IReadOnlyCollection<Kline>>();

        public IReadOnlyCollection<Kline> Klines { get; set; }

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
}