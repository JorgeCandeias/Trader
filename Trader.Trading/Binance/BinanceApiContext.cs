using System.Collections.Generic;
using System.Threading;
using Trader.Data;

namespace Trader.Trading.Binance
{
    internal static class BinanceApiContext
    {
        private static readonly AsyncLocal<bool> _captureUsage = new();
        private static readonly AsyncLocal<IList<Usage>> _usage = new();
        private static readonly AsyncLocal<bool> _skipSigning = new();

        public static IList<Usage>? Usage => _usage.Value;

        public static bool SkipSigning
        {
            get => _skipSigning.Value;
            set => _skipSigning.Value = value;
        }

        public static bool CaptureUsage
        {
            get => _captureUsage.Value;
            set
            {
                if (value)
                {
                    if (_usage.Value is null)
                    {
                        _usage.Value = new List<Usage>();
                    }

                    _captureUsage.Value = true;
                }
                else
                {
                    _usage.Value = null!;
                    _captureUsage.Value = false;
                }
            }
        }
    }
}