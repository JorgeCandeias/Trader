using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands
{
    internal abstract class AlgoCommandBase : IAlgoCommand
    {
        /// <summary>
        /// Serves as a helper base class for algo commands.
        /// </summary>
        protected AlgoCommandBase(Symbol symbol)
        {
            Symbol = symbol ?? ThrowHelper.ThrowArgumentNullException<Symbol>(nameof(symbol));
        }

        public Symbol Symbol { get; }

        public abstract ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default);
    }
}