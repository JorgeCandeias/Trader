using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoContext : IAlgoContext
    {
        private readonly IEnumerable<IAlgoContextConfigurator<AlgoContext>> _configurators;

        public AlgoContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

            _configurators = ServiceProvider.GetRequiredService<IEnumerable<IAlgoContextConfigurator<AlgoContext>>>();
        }

        public string Name { get; internal set; } = string.Empty;

        public Symbol Symbol { get; internal set; } = Symbol.Empty;

        public DateTime TickTime { get; internal set; } = DateTime.MinValue;

        public IServiceProvider ServiceProvider { get; }

        public PositionDetails PositionDetails { get; set; } = PositionDetails.Empty;

        public MiniTicker Ticker { get; set; } = MiniTicker.Empty;

        public Balance AssetSpotBalance { get; set; } = Balance.Empty;

        public Balance QuoteSpotBalance { get; set; } = Balance.Empty;

        public SavingsPosition AssetSavingsBalance { get; set; } = SavingsPosition.Empty;

        public SavingsPosition QuoteSavingsBalance { get; set; } = SavingsPosition.Empty;

        public SwapPoolAssetBalance AssetSwapPoolBalance { get; set; } = SwapPoolAssetBalance.Empty;

        public SwapPoolAssetBalance QuoteSwapPoolBalance { get; set; } = SwapPoolAssetBalance.Empty;

        public IReadOnlyList<OrderQueryResult> Orders { get; set; } = ImmutableList<OrderQueryResult>.Empty;

        public async ValueTask UpdateAsync(CancellationToken cancellationToken = default)
        {
            foreach (var configurator in _configurators)
            {
                await configurator.ConfigureAsync(this, Name, cancellationToken).ConfigureAwait(false);
            }
        }

        #region Static Helpers

        public static AlgoContext Empty { get; } = new AlgoContext(NullServiceProvider.Instance);

        private static readonly AsyncLocal<AlgoContext> _current = new();

        internal static AlgoContext Current
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