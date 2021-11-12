using Outcompute.Trader.Models;
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

        Task SetOrderAsync(OrderQueryResult order, CancellationToken cancellationToken = default);

        #endregion Orders

        #region Klines

        ValueTask SetKlineAsync(Kline item, CancellationToken cancellationToken = default);

        ValueTask SetKlinesAsync(IEnumerable<Kline> items, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<Kline>> GetKlinesAsync(string symbol, KlineInterval interval, DateTime startOpenTime, DateTime endOpenTime, CancellationToken cancellationToken = default);

        #endregion Klines

        #region Tickers

        Task SetTickerAsync(MiniTicker ticker, CancellationToken cancellationToken = default);

        Task<MiniTicker?> TryGetTickerAsync(string symbol, CancellationToken cancellationToken = default);

        #endregion Tickers

        #region Balances

        Task SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default);

        Task<IEnumerable<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default);

        Task<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default);

        #endregion Balances

        #region Trades

        Task SetTradeAsync(AccountTrade trade, CancellationToken cancellationToken = default);

        Task SetTradesAsync(IEnumerable<AccountTrade> trades, CancellationToken cancellationToken = default);

        Task<IEnumerable<AccountTrade>> GetTradesAsync(string symbol, CancellationToken cancellationToken = default);

        #endregion Trades
    }
}