using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks
{
    internal class ClearOpenBuyOrdersBlock : IClearOpenBuyOrdersBlock
    {
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;

        public ClearOpenBuyOrdersBlock(ITradingRepository repository, ITradingService trader, ISystemClock clock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async ValueTask GoAsync(Symbol symbol, CancellationToken cancellationToken = default)
        {
            var orders = await _repository.GetTransientOrdersBySideAsync(symbol.Name, OrderSide.Buy, cancellationToken).ConfigureAwait(false);

            foreach (var order in orders)
            {
                var result = await _trader
                    .CancelOrderAsync(new CancelStandardOrder(symbol.Name, order.OrderId, null, null, null, _clock.UtcNow), cancellationToken)
                    .ConfigureAwait(false);

                await _repository
                    .SetOrderAsync(result, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}