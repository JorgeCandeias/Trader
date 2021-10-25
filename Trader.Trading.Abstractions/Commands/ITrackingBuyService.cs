using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands
{
    public interface ITrackingBuyService
    {
        Task SetTrackingBuyAsync(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings, CancellationToken cancellationToken = default);
    }
}