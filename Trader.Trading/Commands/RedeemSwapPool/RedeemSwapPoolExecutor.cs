using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Commands.RedeemSwapPool
{
    internal class RedeemSwapPoolExecutor : IAlgoCommandExecutor<RedeemSwapPoolCommand, RedeemSwapPoolEvent>
    {
        private readonly ISwapPoolProvider _provider;

        public RedeemSwapPoolExecutor(ISwapPoolProvider provider)
        {
            _provider = provider;
        }

        public ValueTask<RedeemSwapPoolEvent> ExecuteAsync(IAlgoContext context, RedeemSwapPoolCommand command, CancellationToken cancellationToken = default)
        {
            return _provider.RedeemAsync(command.Asset, command.Amount, cancellationToken);
        }
    }
}