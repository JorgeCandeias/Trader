using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
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

        public Task SetKlineAsync(Kline item, CancellationToken cancellationToken = default)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));

            return _grain.SetKlineAsync(item);
        }

        #endregion Klines

        #region Tickers

        public Task<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return _grain.TryGetTickerAsync(symbol);
        }

        public Task SetTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default)
        {
            return _grain.SetTickerAsync(ticker);
        }

        #endregion Tickers

        #region Trades

        public Task<IEnumerable<AccountTrade>> GetTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            return _grain.GetTradesAsync(symbol);
        }

        public Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
        {
            return _grain.SetTradeAsync(trade);
        }

        public Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
        {
            return _grain.SetTradesAsync(trades);
        }

        #endregion Trades

        #region Balances

        public Task SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default)
        {
            return _grain.SetBalancesAsync(balances);
        }

        public Task<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default)
        {
            return _grain.TryGetBalanceAsync(asset);
        }

        #endregion Balances
    }
}