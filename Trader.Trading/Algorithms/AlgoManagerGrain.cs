using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.Timers;

namespace Outcompute.Trader.Trading.Algorithms;

internal sealed partial class AlgoManagerGrain : Grain, IAlgoManagerGrain, IDisposable
{
    private readonly IOptionsMonitor<TraderOptions> _monitor;
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ITimerRegistry _timers;

    public AlgoManagerGrain(IOptionsMonitor<TraderOptions> monitor, ILogger<AlgoManagerGrain> logger, IHostApplicationLifetime lifetime, ITimerRegistry timers)
    {
        _monitor = monitor;
        _logger = logger;
        _lifetime = lifetime;
        _timers = timers;
    }

    private static string TypeName => nameof(AlgoManagerGrain);

    private IDisposable? _pingTimer;
    private IDisposable? _tickTimer;

    public override Task OnActivateAsync()
    {
        var options = _monitor.CurrentValue;

        _pingTimer = _timers.RegisterTimer(this, TickPingAllAlgoGrainsAsync, null, options.PingDelay, options.PingDelay);

        _tickTimer = _timers.RegisterTimer(this, TickExecuteAllAlgosAsync, null, options.BatchTickDelay, options.BatchTickDelay);

        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        Dispose();

        return base.OnDeactivateAsync();
    }

    private async Task TickPingAllAlgoGrainsAsync(object _)
    {
        // snapshot the current options for this tick
        var options = _monitor.CurrentValue;

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

    private async Task TickExecuteAllAlgosAsync(object _)
    {
        // snapshot the current options for this tick
        var options = _monitor.CurrentValue;

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
        var options = _monitor.CurrentValue;

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

    #region Disposable

    public void Dispose()
    {
        // pinning for thread safety
        var pingTimer = _pingTimer;
        if (pingTimer is not null)
        {
            pingTimer.Dispose();
            _pingTimer = null;
        }

        // pinning for thread safety
        var tickTimer = _tickTimer;
        if (tickTimer is not null)
        {
            tickTimer.Dispose();
            _tickTimer = null;
        }

        GC.SuppressFinalize(this);
    }

    ~AlgoManagerGrain()
    {
        Dispose();
    }

    #endregion Disposable

    #region Logging

    [LoggerMessage(0, LogLevel.Error, "{TypeName} failed to ping target algo grain with identity {Identity}")]
    private partial void LogFailedToPingTargetAlgo(Exception ex, string typeName, IGrainIdentity identity);

    [LoggerMessage(1, LogLevel.Error, "{TypeName} failed to tick target algo grain with identity {Identity}")]
    private partial void LogFailedToTickTargetAlgo(Exception ex, string typeName, IGrainIdentity identity);

    #endregion Logging
}