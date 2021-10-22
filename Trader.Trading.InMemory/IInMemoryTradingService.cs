using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.InMemory
{
    public interface IInMemoryTradingService : ITradingService
    {
        public Task SetFlexibleProductPositionsAsync(IEnumerable<SavingsPosition> items);

        public Task SetLeftDailyRedemptionQuotaOnFlexibleProductAsync(string productId, SavingsRedemptionType type, SavingsQuota item);
    }
}