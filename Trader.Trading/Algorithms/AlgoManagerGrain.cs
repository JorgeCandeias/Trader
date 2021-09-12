using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
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

            _options.OnChange(_ => Interlocked.Exchange(ref _changed, 1));
        }

        private int _changed = 1;

        public override Task OnActivateAsync()
        {
            RegisterTimer(_ => TryPingAllAlgoGrainsAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            return base.OnActivateAsync();
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        private async Task TryPingAllAlgoGrainsAsync()
        {
            if (Interlocked.CompareExchange(ref _changed, 0, 1) is 0) return;

            var options = _options.CurrentValue;

            foreach (var name in options.Names)
            {
                var grain = GrainFactory.GetSymbolAlgoHostGrain(name);

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