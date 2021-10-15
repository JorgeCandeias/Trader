using Orleans;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Binance.Tests.Fakes
{
    public class FakeTradingRepository : ITradingRepository
    {
        private readonly IFakeTradingRepositoryGrain _grain;

        public FakeTradingRepository(IGrainFactory factory)
        {
            _grain = factory.GetGrain<IFakeTradingRepositoryGrain>(Guid.Empty);
        }

        public Task<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return _grain.GetKlinesAsync(symbol, interval, startOpenTime, endOpenTime);
        }

        public Task<long> GetLastPagedOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetLastPagedTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<OrderQueryResult?> GetLatestOrderBySideAsync(string symbol, OrderSide side, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetMinTransientOrderIdAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ImmutableSortedOrderSet> GetNonSignificantTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<OrderQueryResult> GetOrderAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ImmutableSortedOrderSet> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ImmutableSortedOrderSet> GetSignificantCompletedOrdersAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<MiniTicker> GetTickerAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ImmutableSortedTradeSet> GetTradesAsync(string symbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ImmutableSortedOrderSet> GetTransientOrdersBySideAsync(string symbol, OrderSide orderSide, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetBalancesAsync(AccountInfo accountInfo, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetKlinesAsync(IEnumerable<Kline> items, CancellationToken cancellationToken = default)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));

            return _grain.SetKlinesAsync(items);
        }

        public Task SetLastPagedOrderIdAsync(string symbol, long orderId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetLastPagedTradeIdAsync(string symbol, long tradeId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetOrderAsync(CancelStandardOrderResult result, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetOrderAsync(OrderResult result, decimal stopPrice = 0, decimal icebergQuantity = 0, decimal originalQuoteOrderQuantity = 0, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetTickersAsync(IEnumerable<MiniTicker> tickers, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return _grain.TryGetKlineAsync(symbol, interval, openTime);
        }
    }
}