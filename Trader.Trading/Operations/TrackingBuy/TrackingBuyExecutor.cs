using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.TrackingBuy
{
    internal class TrackingBuyExecutor : IAlgoResultExecutor<TrackingBuyAlgoResult>
    {
        private readonly ITrackingBuyOperation _operation;

        public TrackingBuyExecutor(ITrackingBuyOperation operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, TrackingBuyAlgoResult result, CancellationToken cancellationToken = default)
        {
            return _operation.SetTrackingBuyAsync(result.Symbol, result.PullbackRatio, result.TargetQuoteBalanceFractionPerBuy, result.MaxNotional, result.RedeemSavings, cancellationToken);
        }
    }
}