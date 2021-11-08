using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Providers;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.RedeemSavings
{
    internal class RedeemSavingsExecutor : IAlgoCommandExecutor<RedeemSavingsCommand, RedeemSavingsEvent>
    {
        private readonly ISavingsProvider _savings;

        public RedeemSavingsExecutor(ISavingsProvider savings)
        {
            _savings = savings;
        }

        public Task<RedeemSavingsEvent> ExecuteAsync(IAlgoContext context, RedeemSavingsCommand command, CancellationToken cancellationToken = default)
        {
            return _savings.RedeemAsync(command.Asset, command.Amount, cancellationToken);
        }
    }
}