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
    internal class NullAlgoContext : IAlgoContext
    {
        private NullAlgoContext()
        {
        }

        public static NullAlgoContext Instance { get; } = new NullAlgoContext();

        public string Name => string.Empty;

        public Symbol Symbol => Symbol.Empty;

        public IServiceProvider ServiceProvider => NullServiceProvider.Instance;

        public PositionDetails PositionDetails => PositionDetails.Empty;

        public MiniTicker Ticker => MiniTicker.Empty;

        public Balance AssetSpotBalance => Balance.Empty;

        public Balance QuoteSpotBalance => Balance.Empty;

        public SavingsPosition AssetSavingsBalance => SavingsPosition.Empty;

        public SavingsPosition QuoteSavingsBalance => SavingsPosition.Empty;

        public IReadOnlyList<OrderQueryResult> Orders => ImmutableList<OrderQueryResult>.Empty;

        public SwapPoolAssetBalance AssetSwapPoolBalance => SwapPoolAssetBalance.Empty;

        public SwapPoolAssetBalance QuoteSwapPoolBalance => SwapPoolAssetBalance.Empty;

        public ValueTask UpdateAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}