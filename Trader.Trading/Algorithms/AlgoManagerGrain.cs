using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            // todo: move these settings to the options class
            RegisterTimer(_ => TryPingAllAlgoGrainsAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            // todo: move these settings to the options class
            RegisterTimer(_ => TryExecuteAllAlgosAsync(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

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

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        private async Task TryExecuteAllAlgosAsync()
        {
            // snapshot the current options for this tick
            var options = _options.CurrentValue;

            // execute all algos ones by one
            foreach (var algo in options.Algos)
            {
                // skip algo non batch algo
                if (!algo.Value.BatchEnabled) continue;

                var grain = GrainFactory.GetAlgoHostGrain(algo.Key);

                try
                {
                    await grain.TickAsync().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "{Grain} failed to tick target algo grain with identity {Identity}",
                        nameof(AlgoManagerGrain), grain.GetGrainIdentity());
                }
            }
        }

        public Task<IEnumerable<AlgoInfo>> GetAlgosAsync()
        {
            var options = _options.CurrentValue;

            // for now this will come from the options snapshot but later it will come from the repository
            var builder = ImmutableList.CreateBuilder<AlgoInfo>();
            foreach (var option in options.Algos)
            {
                builder.Add(new AlgoInfo(
                    option.Key,
                    option.Value.Type,
                    option.Value.Enabled,
                    option.Value.MaxExecutionTime,
                    option.Value.TickDelay,
                    option.Value.TickEnabled));
            }
            return Task.FromResult<IEnumerable<AlgoInfo>>(builder.ToImmutable());
        }
    }
}