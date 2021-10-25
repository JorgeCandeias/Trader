using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.CreateOrder
{
    internal class CreateOrderExecutor : IAlgoCommandExecutor<CreateOrderCommand>
    {
        private readonly ICreateOrderService _operation;

        public CreateOrderExecutor(ICreateOrderService operation)
        {
            _operation = operation;
        }

        public Task ExecuteAsync(IAlgoContext context, CreateOrderCommand result, CancellationToken cancellationToken = default)
        {
            return _operation.CreateOrderAsync(result.Symbol, result.Type, result.Side, result.TimeInForce, result.Quantity, result.Price, result.Tag, cancellationToken);
        }
    }
}