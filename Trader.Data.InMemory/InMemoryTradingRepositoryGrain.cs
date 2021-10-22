using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Data.InMemory
{
    [Reentrant]
    internal class InMemoryTradingRepositoryGrain : Grain, IInMemoryTradingRepositoryGrain
    {
        private readonly Dictionary<string, ImmutableSortedSet<OrderQueryResult>.Builder> _orders = new();
        private readonly Dictionary<(string Symbol, KlineInterval Interval, DateTime OpenTime), Kline> _klines = new();
        private readonly Dictionary<string, MiniTicker> _tickers = new();
        private readonly Dictionary<string, ImmutableSortedSet<AccountTrade>.Builder> _trades = new();
        private readonly Dictionary<string, Balance> _balances = new();

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

        public Task SetKlineAsync(Kline item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            _klines[(item.Symbol, item.Interval, item.OpenTime)] = item;

            return Task.CompletedTask;
        }

        public Task<IEnumerable<OrderQueryResult>> GetOrdersAsync(string symbol)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var result = _orders.TryGetValue(symbol, out var orders)
                ? orders.ToImmutable()
                : ImmutableSortedSet<OrderQueryResult>.Empty;

            return Task.FromResult<IEnumerable<OrderQueryResult>>(result);
        }

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders)
        {
            if (orders is null) throw new ArgumentNullException(nameof(orders));

            foreach (var order in orders)
            {
                SetOrderCore(order);
            }

            return Task.CompletedTask;
        }

        public Task SetOrderAsync(OrderQueryResult order)
        {
            if (order is null) throw new ArgumentNullException(nameof(order));

            SetOrderCore(order);

            return Task.CompletedTask;
        }

        private void SetOrderCore(OrderQueryResult order)
        {
            if (!_orders.TryGetValue(order.Symbol, out var builder))
            {
                _orders[order.Symbol] = builder = ImmutableSortedSet.CreateBuilder(OrderQueryResult.OrderIdComparer);
            }

            builder.Remove(order);
            builder.Add(order);
        }

        public Task SetTickerAsync(MiniTicker ticker)
        {
            if (ticker is null) throw new ArgumentNullException(nameof(ticker));

            SetTickerCore(ticker);

            return Task.CompletedTask;
        }

        public Task SetTickersAsync(IEnumerable<MiniTicker> tickers)
        {
            if (tickers is null) throw new ArgumentNullException(nameof(tickers));

            foreach (var ticker in tickers)
            {
                SetTickerCore(ticker);
            }

            return Task.CompletedTask;
        }

        private void SetTickerCore(MiniTicker ticker)
        {
            _tickers[ticker.Symbol] = ticker;
        }

        public Task<MiniTicker?> TryGetTickerAsync(string symbol)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var ticker = _tickers.TryGetValue(symbol, out var value) ? value : null;

            return Task.FromResult(ticker);
        }

        public Task<IEnumerable<AccountTrade>> GetTradesAsync(string symbol)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            if (_trades.TryGetValue(symbol, out var builder))
            {
                return Task.FromResult<IEnumerable<AccountTrade>>(builder.ToImmutable());
            }

            return Task.FromResult(Enumerable.Empty<AccountTrade>());
        }

        public Task SetTradeAsync(AccountTrade trade)
        {
            if (trade is null) throw new ArgumentNullException(nameof(trade));

            if (!_trades.TryGetValue(trade.Symbol, out var lookup))
            {
                _trades[trade.Symbol] = lookup = ImmutableSortedSet.CreateBuilder(AccountTrade.TradeIdComparer);
            }

            lookup.Remove(trade);
            lookup.Add(trade);

            return Task.CompletedTask;
        }

        public Task SetTradesAsync(IEnumerable<AccountTrade> trades)
        {
            if (trades is null) throw new ArgumentNullException(nameof(trades));

            foreach (var trade in trades)
            {
                if (!_trades.TryGetValue(trade.Symbol, out var lookup))
                {
                    _trades[trade.Symbol] = lookup = ImmutableSortedSet.CreateBuilder(AccountTrade.TradeIdComparer);
                }

                lookup.Remove(trade);
                lookup.Add(trade);
            }

            return Task.CompletedTask;
        }

        public Task SetBalancesAsync(IEnumerable<Balance> balances)
        {
            if (balances is null) throw new ArgumentNullException(nameof(balances));

            foreach (var balance in balances)
            {
                _balances[balance.Asset] = balance;
            }

            return Task.CompletedTask;
        }

        public Task<Balance?> TryGetBalanceAsync(string asset)
        {
            if (asset is null) throw new ArgumentNullException(nameof(asset));

            var balance = _balances.TryGetValue(asset, out var value) ? value : null;

            return Task.FromResult(balance);
        }
    }
}