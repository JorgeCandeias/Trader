using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.RedeemSwapPool
{
    internal class RedeemSwapPoolExecutor : IAlgoCommandExecutor<RedeemSwapPoolCommand, RedeemSwapPoolEvent>
    {
        private readonly ISwapPoolProvider _provider;

        public RedeemSwapPoolExecutor(ISwapPoolProvider provider)
        {
            _provider = provider;
        }

        public Task<RedeemSwapPoolEvent> ExecuteAsync(IAlgoContext context, RedeemSwapPoolCommand command, CancellationToken cancellationToken = default)
        {
            return _provider.RedeemAsync(command.Asset, command.Amount, cancellationToken);
        }
    }
}