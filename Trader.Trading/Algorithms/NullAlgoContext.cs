using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class NullAlgoContext : IAlgoContext
    {
        private NullAlgoContext()
        {
        }

        public string Name => string.Empty;

        public Symbol Symbol => Symbol.Empty;

        public IServiceProvider ServiceProvider => NullServiceProvider.Instance;

        public SignificantResult Significant => SignificantResult.Empty;

        public MiniTicker Ticker => MiniTicker.Empty;

        public static NullAlgoContext Instance { get; } = new NullAlgoContext();
    }
}