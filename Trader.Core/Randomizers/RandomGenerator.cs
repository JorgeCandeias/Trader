using System;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Core.Randomizers
{
    [SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
    internal class RandomGenerator : IRandomGenerator
    {
        private static readonly Random _global = new();

        [ThreadStatic]
        private static Random? _local;

        public int Next(int minValue, int maxValue)
        {
            EnsureLocal();

            return _local!.Next(minValue, maxValue);
        }

        public int Next(int maxValue)
        {
            EnsureLocal();

            return _local!.Next(maxValue);
        }

        public double NextDouble()
        {
            EnsureLocal();

            return _local!.NextDouble();
        }

        private static void EnsureLocal()
        {
            if (_local is null)
            {
                lock (_global)
                {
                    _local = new Random(_global.Next());
                }
            }
        }
    }
}