using Outcompute.Trader.Models;
using Outcompute.Trader.Models.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Data
{
    public interface ITradingRepository
    {
        #region Orders

        Task<IEnumerable<OrderQueryResult>> GetOrdersAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetOrdersAsync(IEnumerable<OrderQueryResult> orders, CancellationToken cancellationToken = default);

        #endregion Orders

        Task<long> GetLastPagedTradeIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetLastPagedTradeIdAsync(string symbol, long tradeId, CancellationToken cancellationToken = default);

        Task<long> GetMaxTradeIdAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default);

        Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default);

        Task<ImmutableSortedTradeSet> GetTradesAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetBalancesAsync(AccountInfo accountInfo, CancellationToken cancellationToken = default);

        Task SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default);

        Task<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default);

        Task SetTickersAsync(IEnumerable<MiniTicker> tickers, CancellationToken cancellationToken = default);

        Task<MiniTicker> GetTickerAsync(string symbol, CancellationToken cancellationToken = default);

        Task SetKlinesAsync(IEnumerable<Kline> items, CancellationToken cancellationToken = default);

        Task<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime, CancellationToken cancellationToken = default);

        Task<Kline?> TryGetKlineAsync(string symbol, KlineInterval interval, DateTime openTime, CancellationToken cancellationToken = default);
    }
}