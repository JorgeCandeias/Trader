using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    internal class AlgoContextPositionsConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        private readonly IOptionsMonitor<AlgoOptions> _monitor;
        private readonly IAutoPositionResolver _resolver;

        public AlgoContextPositionsConfigurator(IOptionsMonitor<AlgoOptions> monitor, IAutoPositionResolver resolver)
        {
            _monitor = monitor;
            _resolver = resolver;
        }

        public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
        {
            var options = _monitor.Get(name);

            context.PositionDetails = await _resolver
                .ResolveAsync(context.Symbol, options.StartTime, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}