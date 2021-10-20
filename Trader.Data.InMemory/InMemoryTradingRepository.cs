using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Data.InMemory
{
    internal class InMemoryTradingRepository : ITradingRepository
    {
        private readonly IInMemoryTradingRepositoryGrain _grain;

        public InMemoryTradingRepository(IGrainFactory factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));

            _grain = factory.GetGrain<IInMemoryTradingRepositoryGrain>(Guid.Empty);
        }

        #region Orders

        public Task<IEnumerable<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return _grain.GetOrdersAsync(symbol);
        }

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
        {
            if (orders is null) throw new ArgumentNullException(nameof(orders));

            return _grain.SetOrdersAsync(orders);
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            if (order is null) throw new ArgumentNullException(nameof(order));

            return _grain.SetOrderAsync(order);
        }

        #endregion Orders

        #region Klines

        public Task<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return _grain.GetKlinesAsync(symbol, interval, startOpenTime, endOpenTime);
        }

        public Task SetKlinesAsync(IEnumerable<Kline> items, CancellationToken cancellationToken = default)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return _grain.SetKlinesAsync(items);
        }

        public Task<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return _grain.TryGetKlineAsync(symbol, interval, openTime);
        }

        public Task SetKlineAsync(Kline item, CancellationToken cancellationToken = default)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            return _grain.SetKlineAsync(item);
        }

        #endregion Klines

        public Task<long> GetLastPagedTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            // todo: remove
            throw new NotImplementedException();
        }

        public Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            // todo: remove
            throw new NotImplementedException();
        }

        public Task<MiniTicker> GetTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            // todo: refactor into try get ticker
            throw new NotImplementedException();
        }

        public Task<ImmutableSortedTradeSet> GetTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            // todo: refactor the result into IEnumerable
            throw new NotImplementedException();
        }

        public Task SetBalancesAsync(AccountInfo accountInfo, CancellationToken cancellationToken = default)
        {
            // todo: remove
            throw new NotImplementedException();
        }

        public Task SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default)
        {
            // todo: implement
            throw new NotImplementedException();
        }

        public Task SetLastPagedTradeIdAsync(string symbol, long tradeId, CancellationToken cancellationToken = default)
        {
            // todo: remove
            throw new NotImplementedException();
        }

        public Task SetTickersAsync(IEnumerable<MiniTicker> tickers, CancellationToken cancellationToken = default)
        {
            // todo: implement
            throw new NotImplementedException();
        }

        public Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
        {
            // todo: remove
            throw new NotImplementedException();
        }

        public Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
        {
            // todo: implement
            throw new NotImplementedException();
        }

        public Task<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default)
        {
            // todo: implement
            throw new NotImplementedException();
        }
    }
}