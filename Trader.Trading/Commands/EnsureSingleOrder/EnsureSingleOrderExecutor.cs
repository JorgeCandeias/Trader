using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.EnsureSingleOrder
{
    internal class EnsureSingleOrderExecutor : IAlgoCommandExecutor<EnsureSingleOrderCommand>
    {
        private readonly IEnsureSingleOrderService _operation;

        public EnsureSingleOrderExecutor(IEnsureSingleOrderService operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, EnsureSingleOrderCommand result, CancellationToken cancellationToken = default)
        {
            return _operation.EnsureSingleOrderAsync(result.Symbol, result.Side, result.Type, result.TimeInForce, result.Quantity, result.Price, result.RedeemSavings, cancellationToken);
        }
    }
}