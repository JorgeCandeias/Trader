using Orleans;
using Orleans.Concurrency;
using Outcompute.Trader.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Data.InMemory
{
    [Reentrant]
    internal class InMemoryTradingRepositoryGrain : Grain, IInMemoryTradingRepositoryGrain
    {
        private readonly Dictionary<string, MiniTicker> _tickers = new();

        private readonly Dictionary<string, Balance> _balances = new();

        private readonly Dictionary<(string Symbol, KlineInterval Interval), ImmutableSortedSet<Kline>.Builder> _klines = new();

        private readonly Dictionary<string, ImmutableSortedSet<AccountTrade>.Builder> _trades = new();

        private readonly Dictionary<string, ImmutableSortedSet<OrderQueryResult>.Builder> _orders = new();

        public ValueTask<ImmutableSortedSet<Kline>> GetKlinesAsync(string symbol, KlineInterval interval)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var result = _klines.TryGetValue((symbol, interval), out var builder)
                ? builder.ToImmutable()
                : ImmutableSortedSet<Kline>.Empty.WithComparer(KlineComparer.Key);

            return ValueTask.FromResult(result);
        }

        public ValueTask SetKlinesAsync(ImmutableList<Kline> items)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                if (!_klines.TryGetValue((item.Symbol, item.Interval), out var builder))
                {
                    _klines[(item.Symbol, item.Interval)] = builder = ImmutableSortedSet.CreateBuilder(KlineComparer.Key);
                }

                builder.Remove(item);
                builder.Add(item);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask SetKlineAsync(Kline item)
        {
            if (!_klines.TryGetValue((item.Symbol, item.Interval), out var builder))
            {
                _klines[(item.Symbol, item.Interval)] = builder = ImmutableSortedSet.CreateBuilder(KlineComparer.Key);
            }

            builder.Remove(item);
            builder.Add(item);

            return ValueTask.CompletedTask;
        }

        public Task<ImmutableSortedSet<OrderQueryResult>> GetOrdersAsync(string symbol)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var result = _orders.TryGetValue(symbol, out var orders)
                ? orders.ToImmutable()
                : ImmutableSortedSet<OrderQueryResult>.Empty.WithComparer(OrderQueryResult.KeyComparer);

            return Task.FromResult(result);
        }

        public Task SetOrdersAsync(ImmutableList<OrderQueryResult> orders)
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
                _orders[order.Symbol] = builder = ImmutableSortedSet.CreateBuilder(OrderQueryResult.KeyComparer);
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

        public Task SetTickersAsync(ImmutableList<MiniTicker> tickers)
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

        public Task<ImmutableSortedSet<AccountTrade>> GetTradesAsync(string symbol)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            var result = _trades.TryGetValue(symbol, out var builder)
                ? builder.ToImmutable()
                : ImmutableSortedSet<AccountTrade>.Empty.WithComparer(AccountTrade.KeyComparer);

            return Task.FromResult(result);
        }

        public Task SetTradeAsync(AccountTrade trade)
        {
            if (trade is null) throw new ArgumentNullException(nameof(trade));

            if (!_trades.TryGetValue(trade.Symbol, out var lookup))
            {
                _trades[trade.Symbol] = lookup = ImmutableSortedSet.CreateBuilder(AccountTrade.KeyComparer);
            }

            lookup.Remove(trade);
            lookup.Add(trade);

            return Task.CompletedTask;
        }

        public Task SetTradesAsync(ImmutableList<AccountTrade> trades)
        {
            if (trades is null) throw new ArgumentNullException(nameof(trades));

            foreach (var trade in trades)
            {
                if (!_trades.TryGetValue(trade.Symbol, out var lookup))
                {
                    _trades[trade.Symbol] = lookup = ImmutableSortedSet.CreateBuilder(AccountTrade.KeyComparer);
                }

                lookup.Remove(trade);
                lookup.Add(trade);
            }

            return Task.CompletedTask;
        }

        public Task SetBalancesAsync(ImmutableList<Balance> balances)
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