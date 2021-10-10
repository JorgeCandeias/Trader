using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Blocks
{
    public interface IClearOpenBuyOrdersBlock
    {
        /// <summary>
        /// Clears all open orders for the specified symbol.
        /// </summary>
        public ValueTask GoAsync(Symbol symbol, CancellationToken cancellationToken = default);
    }
}