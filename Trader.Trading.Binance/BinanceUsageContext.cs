using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance
{
    internal class BinanceUsageContext
    {
        private readonly ConcurrentDictionary<(RateLimitType Type, TimeSpan Window), int> _limits = new();
        private readonly ConcurrentDictionary<(RateLimitType Type, TimeSpan Window), int> _usages = new();

        public void SetLimit(RateLimitType type, TimeSpan window, int limit)
        {
            _limits[(type, window)] = limit;
        }

        public void SetUsed(RateLimitType type, TimeSpan window, int used)
        {
            _usages[(type, window)] = used;
        }

        public IEnumerable<(RateLimitType Type, TimeSpan Window, int Limit, int Used)> EnumerateAll()
        {
            foreach (var limit in _limits)
            {
                if (_usages.TryGetValue(limit.Key, out var used))
                {
                    yield return (limit.Key.Type, limit.Key.Window, limit.Value, used);
                }
            }
        }
    }
}