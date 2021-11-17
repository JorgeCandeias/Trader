using Microsoft.Extensions.Options;

namespace Outcompute.Trader.Trading.Algorithms.Context.Configurators
{
    internal class AlgoContextOptionsConfigurator : IAlgoContextConfigurator<AlgoContext>
    {
        private readonly IOptionsMonitor<AlgoOptions> _options;

        public AlgoContextOptionsConfigurator(IOptionsMonitor<AlgoOptions> options)
        {
            _options = options;
        }

        public ValueTask ConfigureAsync(AlgoContext context, string name, CancellationToken cancellationToken = default)
        {
            context.Options = _options.Get(name);

            return ValueTask.CompletedTask;
        }
    }
}