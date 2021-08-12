using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading.Algorithms.Steps
{
    public interface ITrackingBuyStep
    {
        Task<bool> GoAsync(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, CancellationToken cancellationToken = default);
    }
}