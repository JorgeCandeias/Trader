using Outcompute.Trader.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
    public interface IAveragingSellService
    {
        Task SetAveragingSellAsync(Symbol symbol, IReadOnlyCollection<OrderQueryResult> orders, decimal profitMultiplier, bool redeemSavings, CancellationToken cancellationToken = default);
    }
}