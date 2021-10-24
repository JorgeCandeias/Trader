using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.SignificantAveragingSell
{
    internal class SignificantAveragingSellExecutor : IAlgoResultExecutor<SignificantAveragingSellAlgoResult>
    {
        private readonly ISignificantAveragingSellOperation _operation;

        public SignificantAveragingSellExecutor(ISignificantAveragingSellOperation operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, SignificantAveragingSellAlgoResult result, CancellationToken cancellationToken = default)
        {
            return _operation.SetSignificantAveragingSellAsync(result.Symbol, result.Ticker, result.Orders, result.MinimumProfitRate, result.RedeemSavings, cancellationToken);
        }
    }
}