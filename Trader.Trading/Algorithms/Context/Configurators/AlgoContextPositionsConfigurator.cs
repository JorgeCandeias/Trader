using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    internal class AlgoContextPositionsConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        private readonly IOptionsMonitor<AlgoOptions> _options;
        private readonly IAutoPositionResolver _resolver;

        public AlgoContextPositionsConfigurator(IOptionsMonitor<AlgoOptions> monitor, IAutoPositionResolver resolver)
        {
            _options = monitor;
            _resolver = resolver;
        }

        public async ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
        {
            if (IsNullOrEmpty(context.Symbol.Name))
            {
                return;
            }

            var options = _options.Get(name);

            context.PositionDetails = await _resolver
                .ResolveAsync(context.Symbol, options.StartTime, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}