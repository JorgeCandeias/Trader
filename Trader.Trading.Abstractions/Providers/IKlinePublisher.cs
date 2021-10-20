using Outcompute.Trader.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    /// <summary>
    /// Abstraction for kline publisher components.
    /// </summary>
    public interface IKlinePublisher
    {
        /// <summary>
        /// Publishes a kline to consumers.
        /// </summary>
        ValueTask PublishAsync(Kline item, CancellationToken cancellationToken = default);
    }
}