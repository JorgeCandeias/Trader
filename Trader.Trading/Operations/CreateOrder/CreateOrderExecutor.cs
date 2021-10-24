using Outcompute.Trader.Trading.Algorithms;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Operations.CreateOrder
{
    internal class CreateOrderExecutor : IAlgoResultExecutor<CreateOrderAlgoResult>
    {
        private readonly ICreateOrderOperation _operation;

        public CreateOrderExecutor(ICreateOrderOperation operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, CreateOrderAlgoResult result, CancellationToken cancellationToken = default)
        {
            return _operation.CreateOrderAsync(result.Symbol, result.Type, result.Side, result.TimeInForce, result.Quantity, result.Price, result.Tag, cancellationToken);
        }
    }
}