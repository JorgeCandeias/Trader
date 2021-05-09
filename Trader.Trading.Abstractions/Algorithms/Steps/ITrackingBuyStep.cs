using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading.Algorithms.Steps
{
    public interface ITrackingBuyStep
    {
        Task GoAsync(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, CancellationToken cancellationToken = default);
    }
}