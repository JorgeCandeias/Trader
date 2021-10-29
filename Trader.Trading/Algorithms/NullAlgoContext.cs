using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

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

        public SignificantResult Significant => SignificantResult.Empty;

        public MiniTicker Ticker => MiniTicker.Empty;

        public Balance AssetSpotBalance => Balance.Empty;

        public Balance QuoteSpotBalance => Balance.Empty;

        public SavingsPosition AssetSavingsBalance => SavingsPosition.Empty;

        public SavingsPosition QuoteSavingsBalance => SavingsPosition.Empty;

        public IReadOnlyList<OrderQueryResult> Orders => ImmutableList<OrderQueryResult>.Empty;
    }
}