using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoContext : IAlgoContext
    {
        public AlgoContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public string Name { get; internal set; } = string.Empty;

        public Symbol Symbol { get; internal set; } = Symbol.Empty;

        public IServiceProvider ServiceProvider { get; }

        public SignificantResult Significant { get; set; } = SignificantResult.Empty;

        public MiniTicker Ticker { get; set; } = MiniTicker.Empty;

        public Balance AssetBalance { get; set; } = Balance.Empty;

        public Balance QuoteBalance { get; set; } = Balance.Empty;

        public static AlgoContext Empty { get; } = new AlgoContext(NullServiceProvider.Instance);
    }
}