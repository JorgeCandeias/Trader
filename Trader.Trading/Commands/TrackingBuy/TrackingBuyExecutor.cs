using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.TrackingBuy
{
    internal class TrackingBuyExecutor : IAlgoCommandExecutor<TrackingBuyCommand>
    {
        private readonly ITrackingBuyService _operation;

        public TrackingBuyExecutor(ITrackingBuyService operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, TrackingBuyCommand result, CancellationToken cancellationToken = default)
        {
            return _operation.SetTrackingBuyAsync(result.Symbol, result.PullbackRatio, result.TargetQuoteBalanceFractionPerBuy, result.MaxNotional, result.RedeemSavings, cancellationToken);
        }
    }
}