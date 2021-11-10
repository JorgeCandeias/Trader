using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    public interface IAlgoContextConfigurator<in TAlgoContext>
        where TAlgoContext : IAlgoContext
    {
        ValueTask ConfigureAsync(TAlgoContext context, string name, CancellationToken cancellationToken = default);
    }
}