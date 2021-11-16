using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.Runtime;
using Outcompute.Trader.Core.Randomizers;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Trading.Watchdog;

internal class WatchdogService : BackgroundService
{
    private readonly WatchdogOptions _options;
    private readonly ILogger _logger;
    private readonly IWatchdogEntry[] _entries;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ISiloStatusOracle _oracle;
    private readonly IRandomGenerator _random;
    private readonly IServiceProvider _provider;

    public WatchdogService(IOptions<WatchdogOptions> options, ILogger<WatchdogService> logger, IEnumerable<IWatchdogEntry> entries, IHostApplicationLifetime lifetime, ISiloStatusOracle oracle, IRandomGenerator random, IServiceProvider provider)
    {
        _options = options.Value;
        _logger = logger;
        _entries = entries.ToArray();
        _lifetime = lifetime;
        _oracle = oracle;
        _random = random;
        _provider = provider;
    }

    private readonly ActiveSiloCounter _activeSiloCounter = new();

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // keep track of the silo count to sparse the pings when the cluster grows
        if (!_oracle.SubscribeToSiloStatusEvents(_activeSiloCounter))
        {
            throw new WatchdogException("Could not subscribe to silo status events");
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
            _logger.PingingTargets(_entries.Length);

            var buffer = ArrayPool<Task>.Shared.Rent(_entries.Length);

            for (var i = 0; i < _entries.Length; i++)
            {
                buffer[i] = _entries[i].ExecuteAsync(_provider, stoppingToken);
            }

            for (var i = 0; i < _entries.Length; i++)
            {
                try
                {
                    await buffer[i].ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.PingTargetFailed(ex);
                }
            }

            ArrayPool<Task>.Shared.Return(buffer);

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

    private sealed class ActiveSiloCounter : ISiloStatusListener
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
        _started(logger, nameof(WatchdogService), null!);
    }

    private static readonly Action<ILogger, string, Exception> _stopped = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(0, nameof(Stopped)),
        "{Service} stopped");

    public static void Stopped(this ILogger logger)
    {
        _stopped(logger, nameof(WatchdogService), null!);
    }

    private static readonly Action<ILogger, string, int, Exception> _pingingTargets = LoggerMessage.Define<string, int>(
        LogLevel.Debug,
        new EventId(0, nameof(PingingTargets)),
        "{Service} pinging {TargetCount} targets");

    public static void PingingTargets(this ILogger logger, int count)
    {
        _pingingTargets(logger, nameof(WatchdogService), count, null!);
    }

    private static readonly Action<ILogger, string, IGrainIdentity, Exception> _pingingTarget = LoggerMessage.Define<string, IGrainIdentity>(
        LogLevel.Debug,
        new EventId(0, nameof(PingingTarget)),
        "{Service} pinging target with identity {Identity}");

    public static void PingingTarget(this ILogger logger, IGrainIdentity identity)
    {
        _pingingTarget(logger, nameof(WatchdogService), identity, null!);
    }

    private static readonly Action<ILogger, string, IGrainIdentity, Exception> _pingedTarget = LoggerMessage.Define<string, IGrainIdentity>(
        LogLevel.Debug,
        new EventId(0, nameof(PingedTarget)),
        "{Service} pinged target with identity {Identity} successfuly");

    public static void PingedTarget(this ILogger logger, IGrainIdentity identity)
    {
        _pingedTarget(logger, nameof(WatchdogService), identity, null!);
    }

    private static readonly Action<ILogger, string, Exception> _pingTargetFailed = LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(0, nameof(PingTargetFailed)),
        "{Service} failed to ping target entry");

    public static void PingTargetFailed(this ILogger logger, Exception exception)
    {
        _pingTargetFailed(logger, nameof(WatchdogService), exception);
    }
}