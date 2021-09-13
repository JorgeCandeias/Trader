using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoManagerGrain : Grain, IAlgoManagerGrain
    {
        private readonly IOptionsMonitor<AlgoManagerGrainOptions> _options;
        private readonly ILogger _logger;

        public AlgoManagerGrain(IOptionsMonitor<AlgoManagerGrainOptions> options, ILogger<AlgoManagerGrain> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task OnActivateAsync()
        {
            RegisterTimer(_ => TryPingAllAlgoGrainsAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            return base.OnActivateAsync();
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        private async Task TryPingAllAlgoGrainsAsync()
        {
            // snapshot the current options for this tick
            var options = _options.CurrentValue;

            // ping all enabled algos
            foreach (var algo in options.Algos)
            {
                // skip disabled algo
                if (!algo.Value.Enabled) continue;

                var grain = GrainFactory.GetAlgoHostGrain(algo.Key);

                try
                {
                    await grain.PingAsync().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "{Grain} failed to ping target algo grain with identity {Identity}",
                        nameof(AlgoManagerGrain), grain.GetGrainIdentity());
                }
            }
        }
    }
}