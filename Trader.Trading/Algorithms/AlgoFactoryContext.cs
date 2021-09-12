using System;
using System.Threading;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal static class AlgoFactoryContext
    {
        private static readonly AsyncLocal<string> _algoName = new();

        public static string AlgoName
        {
            get
            {
                return _algoName.Value ?? string.Empty;
            }
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value));

                _algoName.Value = value;
            }
        }
    }
}