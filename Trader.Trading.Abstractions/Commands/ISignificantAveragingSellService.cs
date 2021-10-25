using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
    public interface ISignificantAveragingSellService
    {
        Task SetSignificantAveragingSellAsync(Symbol symbol, MiniTicker ticker, IReadOnlyCollection<OrderQueryResult> orders, decimal minimumProfitRate, bool redeemSavings, CancellationToken cancellationToken = default);
    }
}