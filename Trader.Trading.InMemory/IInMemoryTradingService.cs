using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.InMemory
{
    public interface IInMemoryTradingService : ITradingService
    {
        #region Exchange

        Task SetExchangeInfoAsync(ExchangeInfo info, CancellationToken cancellationToken = default);

        #endregion Exchange

        Task SetFlexibleProductPositionsAsync(IEnumerable<SavingsPosition> items);

        Task SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, SavingsRedemptionType type, SavingsQuota item);

        Task Set24hTickerPriceChangeStatisticsAsync(Ticker ticker, CancellationToken cancellationToken = default);

        Task SetAccountInfoAsync(AccountInfo info, CancellationToken cancellationToken = default);
    }
}