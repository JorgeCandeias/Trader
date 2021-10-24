using Outcompute.Trader.Core;
using Outcompute.Trader.Models;
using System;
using System.Threading;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoContext : IAlgoContext
    {
        private static readonly AsyncLocal<AlgoContext> _current = new();

        public AlgoContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public string Name { get; internal set; } = string.Empty;

        public Symbol Symbol { get; internal set; } = Symbol.Empty;

        public IServiceProvider ServiceProvider { get; }

        public static AlgoContext Empty { get; } = new AlgoContext(NullServiceProvider.Instance);

        public static AlgoContext Current
        {
            get
            {
                return _current.Value ?? Empty;
            }
            set
            {
                _current.Value = value;
            }
        }
    }
}