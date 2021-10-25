using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.SignificantAveragingSell
{
    internal class SignificantAveragingSellExecutor : IAlgoCommandExecutor<SignificantAveragingSellCommand>
    {
        private readonly ISignificantAveragingSellService _operation;

        public SignificantAveragingSellExecutor(ISignificantAveragingSellService operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, SignificantAveragingSellCommand result, CancellationToken cancellationToken = default)
        {
            return _operation.SetSignificantAveragingSellAsync(result.Symbol, result.Ticker, result.Orders, result.MinimumProfitRate, result.RedeemSavings, cancellationToken);
        }
    }
}