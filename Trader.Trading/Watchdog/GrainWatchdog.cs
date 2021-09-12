using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Core;
using Orleans.Runtime;
using Outcompute.Trader.Core.Randomizers;
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
        private readonly ISiloStatusOracle _oracle;
        private readonly IRandomGenerator _random;
        private readonly IGrainFactory _factory;

        public GrainWatchdog(IOptions<GrainWatchdogOptions> options, ILogger<GrainWatchdog> logger, IEnumerable<IGrainWatchdogEntry> entries, IHostApplicationLifetime lifetime, ISiloStatusOracle oracle, IRandomGenerator random, IGrainFactory factory)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _entries = entries ?? throw new ArgumentNullException(nameof(entries));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _oracle = oracle ?? throw new ArgumentNullException(nameof(oracle));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private readonly List<IWatchdogGrainExtension> _extensions = new();
        private readonly List<IGrainIdentity> _identities = new();
        private readonly ActiveSiloCounter _activeSiloCounter = new();

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // cache the extension proxies to avoid redundant garbage during execution
            foreach (var entry in _entries)
            {
                var grain = entry.GetGrain(_factory);

                _extensions.Add(grain.AsReference<IWatchdogGrainExtension>());
                _identities.Add(grain.GetGrainIdentity());
            }

            // keep track of the silo count to sparse the pings when the cluster grows
            if (!_oracle.SubscribeToSiloStatusEvents(_activeSiloCounter))
            {
                throw new GrainWatchdogException("Could not subscribe to silo status events");
            }

            // seed the counter with the current silo statuses
            foreach (var status in _oracle.GetApproximateSiloStatuses(true))
            {
                _activeSiloCounter.SiloStatusChangeNotification(status.Key, status.Value);
            }

            _logger.Started();

            return base.StartAsync(cancellationToken);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await ApplicationStartedAsync(stoppingToken).ConfigureAwait(false);

            // keep pinging all targets
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.PingingTargets(_extensions.Count);

                var buffer = ArrayPool<Task>.Shared.Rent(_extensions.Count);

                for (var i = 0; i < _extensions.Count; i++)
                {
                    _logger.PingingTarget(_identities[i]);

                    buffer[i] = _extensions[i].PingAsync();
                }

                for (var i = 0; i < _extensions.Count; i++)
                {
                    try
                    {
                        await buffer[i].ConfigureAwait(false);

                        _logger.PingedTarget(_identities[i]);
                    }
                    catch (Exception ex)
                    {
                        _logger.PingTargetFailed(_identities[i], ex);
                    }
                }

                ArrayPool<Task>.Shared.Return(buffer, true);

                await DelayTickAsync(stoppingToken).ConfigureAwait(false);
            }
        }

        private Task DelayTickAsync(CancellationToken stoppingToken)
        {
            // multiply the tick delay by the number of active silos
            var delay = _options.TickDelay * (_activeSiloCounter.Count is 0 ? 1 : _activeSiloCounter.Count);

            // add a random delay to help sparse the calls over time across the cluster
            delay += delay * _options.TickDelayRandomFactor * _random.NextDouble();

            // wait for the next tick now
            return Task.Delay(delay, stoppingToken);
        }

        private async Task ApplicationStartedAsync(CancellationToken stoppingToken)
        {
            var wait = new TaskCompletionSource();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStarted, stoppingToken);
            using var reg = linked.Token.Register(() => wait.SetResult());

            await wait.Task.ConfigureAwait(false);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Stopped();

            return base.StopAsync(cancellationToken);
        }

        private class ActiveSiloCounter : ISiloStatusListener
        {
            private readonly HashSet<SiloAddress> _silos = new();

            public void SiloStatusChangeNotification(SiloAddress updatedSilo, SiloStatus status)
            {
                switch (status)
                {
                    case SiloStatus.Active:
                        _silos.Add(updatedSilo);
                        break;

                    default:
                        _silos.Remove(updatedSilo);
                        break;
                }
            }

            public int Count => _silos.Count;
        }
    }

    internal static class GrainWatchdogLoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception> _started = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(0, nameof(Started)),
            "{Service} started");

        public static void Started(this ILogger logger)
        {
            _started(logger, nameof(GrainWatchdog), null!);
        }

        private static readonly Action<ILogger, string, Exception> _stopped = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(0, nameof(Stopped)),
            "{Service} stopped");

        public static void Stopped(this ILogger logger)
        {
            _stopped(logger, nameof(GrainWatchdog), null!);
        }

        private static readonly Action<ILogger, string, int, Exception> _pingingTargets = LoggerMessage.Define<string, int>(
            LogLevel.Debug,
            new EventId(0, nameof(PingingTargets)),
            "{Service} pinging {TargetCount} targets");

        public static void PingingTargets(this ILogger logger, int count)
        {
            _pingingTargets(logger, nameof(GrainWatchdog), count, null!);
        }

        private static readonly Action<ILogger, string, IGrainIdentity, Exception> _pingingTarget = LoggerMessage.Define<string, IGrainIdentity>(
            LogLevel.Debug,
            new EventId(0, nameof(PingingTarget)),
            "{Service} pinging target with identity {Identity}");

        public static void PingingTarget(this ILogger logger, IGrainIdentity identity)
        {
            _pingingTarget(logger, nameof(GrainWatchdog), identity, null!);
        }

        private static readonly Action<ILogger, string, IGrainIdentity, Exception> _pingedTarget = LoggerMessage.Define<string, IGrainIdentity>(
            LogLevel.Debug,
            new EventId(0, nameof(PingedTarget)),
            "{Service} pinged target with identity {Identity} successfuly");

        public static void PingedTarget(this ILogger logger, IGrainIdentity identity)
        {
            _pingedTarget(logger, nameof(GrainWatchdog), identity, null!);
        }

        private static readonly Action<ILogger, string, IGrainIdentity, Exception> _pingTargetFailed = LoggerMessage.Define<string, IGrainIdentity>(
            LogLevel.Error,
            new EventId(0, nameof(PingTargetFailed)),
            "{Service} failed to ping target entry with identity {Identity}");

        public static void PingTargetFailed(this ILogger logger, IGrainIdentity identity, Exception exception)
        {
            _pingTargetFailed(logger, nameof(GrainWatchdog), identity, exception);
        }
    }
}