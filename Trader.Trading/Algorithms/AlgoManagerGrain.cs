using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        }

        private readonly CancellationTokenSource _cancellation = new();

        public override Task OnActivateAsync()
        {
            var options = _options.CurrentValue;

            RegisterTimer(_ => TickPingAllAlgoGrainsAsync(), null, options.PingDelay, options.PingDelay);

            RegisterTimer(_ => TickExecuteAllAlgosAsync(), null, options.BatchTickDelay, options.BatchTickDelay);

            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _cancellation.Dispose();

            return base.OnDeactivateAsync();
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        private async Task TickPingAllAlgoGrainsAsync()
        {
            // snapshot the current options for this tick
            var options = _options.CurrentValue;

            // ping all enabled algos
            foreach (var algo in options.Algos)
            {
                // break on deactivation
                if (_cancellation.IsCancellationRequested) return;

                // skip disabled algo
                if (!algo.Value.Enabled) continue;

                var grain = GrainFactory.GetAlgoHostGrain(algo.Key);

                try
                {
                    await grain.PingAsync();
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
        private async Task TickExecuteAllAlgosAsync()
        {
            // snapshot the current options for this tick
            var options = _options.CurrentValue;

            // execute all algos ones by one
            foreach (var algo in options.Algos)
            {
                // break on deactivation
                if (_cancellation.IsCancellationRequested) return;

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

            // for now this will come from the options snapshot but later it will come from a more sophisticated algo settings provider
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