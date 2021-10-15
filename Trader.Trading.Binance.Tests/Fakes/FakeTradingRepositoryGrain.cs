using Orleans;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Tests.Fakes
{
    internal class FakeTradingRepositoryGrain : Grain, IFakeTradingRepositoryGrain
    {
        private readonly Dictionary<(string Symbol, KlineInterval Interval, DateTime OpenTime), Kline> _klines = new Dictionary<(string Symbol, KlineInterval Interval, DateTime OpenTime), Kline>();

        public Task<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var result = _klines.Values
                .Where(x => x.Symbol == symbol && x.Interval == interval && x.OpenTime >= startOpenTime && x.OpenTime <= endOpenTime)
                .ToImmutableList();

            return Task.FromResult<IEnumerable<Kline>>(result);
        }

        public Task SetKlinesAsync(IEnumerable<Kline> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                _klines[(item.Symbol, item.Interval, item.OpenTime)] = item;
            }

            return Task.CompletedTask;
        }

        public Task<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            if (_klines.TryGetValue((symbol, interval, openTime), out var value))
            {
                return Task.FromResult<Kline?>(value);
            }

            return Task.FromResult<Kline?>(null);
        }
    }
}