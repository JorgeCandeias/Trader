using Outcompute.Trader.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Outcompute.Trader.Trading.Binance
{
    internal class BinanceUsageContext
    {
        private readonly ConcurrentDictionary<(RateLimitType Type, TimeSpan Window), int> _limits = new();
        private readonly ConcurrentDictionary<(RateLimitType Type, TimeSpan Window), (int Used, DateTime Updated)> _usages = new();

        public void SetLimit(RateLimitType type, TimeSpan window, int limit)
        {
            _limits[(type, window)] = limit;
        }

        public void SetUsed(RateLimitType type, TimeSpan window, int used, DateTime updated)
        {
            _usages[(type, window)] = (used, updated);
        }

        public IEnumerable<(RateLimitType Type, TimeSpan Window, int Limit, int Used, DateTime Updated)> EnumerateAll()
        {
            foreach (var limit in _limits)
            {
                if (_usages.TryGetValue(limit.Key, out var used))
                {
                    yield return (limit.Key.Type, limit.Key.Window, limit.Value, used.Used, used.Updated);
                }
            }
        }
    }
}