using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal partial class AlgoManagerGrain : Grain, IAlgoManagerGrain
    {
        private readonly IOptionsMonitor<TraderOptions> _options;
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public AlgoManagerGrain(IOptionsMonitor<TraderOptions> options, ILogger<AlgoManagerGrain> logger, IHostApplicationLifetime lifetime)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        private static string TypeName => nameof(AlgoManagerGrain);

        public override Task OnActivateAsync()
        {
            var options = _options.CurrentValue;

            RegisterTimer(TickPingAllAlgoGrainsAsync, null, options.PingDelay, options.PingDelay);

            RegisterTimer(TickExecuteAllAlgosAsync, null, options.BatchTickDelay, options.BatchTickDelay);

            return base.OnActivateAsync();
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        private async Task TickPingAllAlgoGrainsAsync(object _)
        {
            // snapshot the current options for this tick
            var options = _options.CurrentValue;

            // ping all enabled algos
            foreach (var algo in options.Algos)
            {
                // break early on app shutdown
                if (_lifetime.ApplicationStopping.IsCancellationRequested)
                {
                    return;
                }

                // skip disabled algo
                if (!algo.Value.Enabled)
                {
                    continue;
                }

                var grain = GrainFactory.GetAlgoHostGrain(algo.Key);

                try
                {
                    await grain.PingAsync();
                }
                catch (Exception ex)
                {
                    LogFailedToPingTargetAlgo(ex, TypeName, grain.GetGrainIdentity());
                }
            }
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        private async Task TickExecuteAllAlgosAsync(object _)
        {
            // snapshot the current options for this tick
            var options = _options.CurrentValue;

            // execute all algos one by one
            foreach (var algo in options.Algos.OrderBy(x => x.Value.BatchOrder).ThenBy(x => x.Key))
            {
                // break early on app shutdown
                if (_lifetime.ApplicationStopping.IsCancellationRequested) return;

                // skip disabled algo
                if (!algo.Value.Enabled) continue;

                // skip algo non batch algo
                if (!algo.Value.BatchEnabled) continue;

                var grain = GrainFactory.GetAlgoHostGrain(algo.Key);

                try
                {
                    await grain.TickAsync().ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    LogFailedToTickTargetAlgo(ex, TypeName, grain.GetGrainIdentity());
                }
            }
        }

        public Task<IReadOnlyCollection<AlgoInfo>> GetAlgosAsync()
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
            return Task.FromResult<IReadOnlyCollection<AlgoInfo>>(builder.ToImmutable());
        }

        #region Logging

        [LoggerMessage(0, LogLevel.Error, "{TypeName} failed to ping target algo grain with identity {Identity}")]
        private partial void LogFailedToPingTargetAlgo(Exception ex, string typeName, IGrainIdentity identity);

        [LoggerMessage(0, LogLevel.Error, "{TypeName} failed to tick target algo grain with identity {Identity}")]
        private partial void LogFailedToTickTargetAlgo(Exception ex, string typeName, IGrainIdentity identity);

        #endregion Logging
    }
}