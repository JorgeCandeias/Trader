using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Watchdog
{
    internal class GrainWatchdog : BackgroundService
    {
        private readonly GrainWatchdogOptions _options;
        private readonly ILogger _logger;
        private readonly IEnumerable<IGrainWatchdogEntry> _entries;
        private readonly IHostApplicationLifetime _lifetime;

        public GrainWatchdog(IOptions<GrainWatchdogOptions> options, ILogger<GrainWatchdog> logger, IEnumerable<IGrainWatchdogEntry> entries, IHostApplicationLifetime lifetime)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        private readonly List<IWatchdogGrainExtension> _extensions = new();
        private readonly List<IGrainIdentity> _identities = new();

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var entry in _entries)
            {
                var grain = entry.GetGrain();

                _extensions.Add(grain.AsReference<IWatchdogGrainExtension>());
                _identities.Add(grain.GetGrainIdentity());
            }

            _logger.LogInformation("{Service} started", nameof(GrainWatchdog));

            return base.StartAsync(cancellationToken);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // wait for the application to stabilize
            var wait = new TaskCompletionSource();
            using (var reg = _lifetime.ApplicationStarted.Register(() => wait.SetResult()))
            {
                await wait.Task.ConfigureAwait(false);
            }

            // keep pinging all targets
            while (!stoppingToken.IsCancellationRequested)
            {
                var buffer = ArrayPool<Task>.Shared.Rent(_extensions.Count);

                for (var i = 0; i < _extensions.Count; i++)
                {
                    buffer[i] = _extensions[i].PingAsync();
                }

                for (var i = 0; i < _extensions.Count; i++)
                {
                    try
                    {
                        await buffer[i].ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "{Service} failed to ping target entry with identity {Identity}",
                            nameof(GrainWatchdog), _identities[i]);
                    }
                }

                ArrayPool<Task>.Shared.Return(buffer, true);

                // todo: widen this delay by the number of active silos
                await Task.Delay(_options.TickDelay, stoppingToken).ConfigureAwait(false);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Service} stopped", nameof(GrainWatchdog));

            return base.StopAsync(cancellationToken);
        }
    }
}