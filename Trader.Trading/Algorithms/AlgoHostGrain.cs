using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Orleans.Timers;
using Outcompute.Trader.Trading.Readyness;

namespace Outcompute.Trader.Trading.Algorithms;

/// <inheritdoc cref="IAlgoHostGrain" />
internal sealed partial class AlgoHostGrain : Grain, IAlgoHostGrainInternal, IDisposable
{
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<AlgoOptions> _options;
    private readonly IGrainActivationContext _context;
    private readonly IReadynessProvider _readyness;
    private readonly IServiceScope _scope;
    private readonly IAlgoStatisticsPublisher _publisher;
    private readonly ITimerRegistry _timers;

    public AlgoHostGrain(ILogger<AlgoHostGrain> logger, IOptionsMonitor<AlgoOptions> options, IGrainActivationContext context, IReadynessProvider readyness, IAlgoStatisticsPublisher publisher, IServiceProvider provider, ITimerRegistry timers)
    {
        _logger = logger;
        _options = options;
        _context = context;
        _readyness = readyness;
        _publisher = publisher;
        _timers = timers;

        _scope = provider.CreateScope();
    }

    private const string TypeName = nameof(AlgoHostGrain);

    private readonly CancellationTokenSource _cancellation = new();

    private string _name = Empty;
    private IAlgo _algo = NoopAlgo.Instance;
    private IDisposable? _executionTimer;
    private IDisposable? _readynessTimer;
    private bool _ready;
    private bool _loggedNotReady;

    public override async Task OnActivateAsync()
    {
        // the name of the algo is the key for this grain instance
        _name = _context.GrainIdentity.PrimaryKeyString;

        // snapshot the current algo host options
        var options = _options.Get(_name);

        // resolve the factory for the current algo type and create the algo instance
        _algo = _scope.ServiceProvider.GetRequiredService<IAlgoFactoryResolver>().Resolve(options.Type).Create(_name);

        // run startup work
        await _algo.StartAsync(_cancellation.Token);

        // keep the execution behaviour updated upon options change
        _options.OnChange(_ => this.AsReference<IAlgoHostGrainInternal>().InvokeOneWay(x => x.ApplyOptionsAsync()));

        // apply the options now
        await ApplyOptionsAsync();

        // spin up the readyness check
        _readynessTimer = _timers.RegisterTimer(this, _ => TickReadynessAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        LogStarted(TypeName, _name);
    }

    public override async Task OnDeactivateAsync()
    {
        StopTicking();

        _cancellation.Cancel();

        try
        {
            var options = _options.Get(_name);
            using var stopCancellation = new CancellationTokenSource(options.StopTimeout);
            await _algo.StopAsync(stopCancellation.Token);
        }
        finally
        {
            LogStopped(TypeName, _name);
        }

        await base.OnDeactivateAsync();
    }

    public Task PingAsync() => Task.CompletedTask;

    public Task ApplyOptionsAsync()
    {
        var options = _options.Get(_name);

        // enable or disable timer based execution as needed
        if (options.Enabled && options.TickEnabled)
        {
            StartTicking(options);
        }
        else
        {
            StopTicking();
        }

        return Task.CompletedTask;
    }

    private void StartTicking(AlgoOptions options)
    {
        if (_executionTimer is null)
        {
            _executionTimer = RegisterTimer(_ => ExecuteAlgoAsync(), null, options.TickDelay, options.TickDelay);
        }
    }

    private void StopTicking()
    {
        if (_executionTimer is not null)
        {
            _executionTimer.Dispose();
            _executionTimer = null;
        }
    }

    private async Task ExecuteAlgoAsync()
    {
        // skip if the system is not ready
        if (!_ready)
        {
            if (!_loggedNotReady)
            {
                LogWaiting(TypeName, _name);
                _loggedNotReady = true;
            }
            return;
        }
        _loggedNotReady = false;

        // snapshot the options for this execution
        var options = _options.Get(_name);

        // enforce the execution time limit along with grain cancellation
        using var limit = new CancellationTokenSource(options.MaxExecutionTime);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(limit.Token, _cancellation.Token);

        // execute the algo under the limits
        var result = await _algo.GoAsync(linked.Token);

        // execute the algo result under the limits
        await result.ExecuteAsync(_algo.Context, linked.Token);

        // todo: refactor this into a post-execution registration set called by the algo base class itself
        // publish current algo statistics
        foreach (var symbol in _algo.Context.Symbols)
        {
            var data = _algo.Context.Data[symbol.Name];

            await _publisher.PublishAsync(data.AutoPosition, data.Ticker, linked.Token);
        }
    }

    public Task TickAsync()
    {
        return ExecuteAlgoAsync();
    }

    private async Task TickReadynessAsync()
    {
        try
        {
            _ready = await _readyness.IsReadyAsync(_cancellation.Token);
        }
        catch
        {
            _ready = false;
            throw;
        }
    }

    public void Dispose()
    {
        _cancellation.Dispose();
        _executionTimer?.Dispose();
        _readynessTimer?.Dispose();
    }

    #region Logging

    [LoggerMessage(1, LogLevel.Information, "{Type} {Name} started")]
    private partial void LogStarted(string type, string name);

    [LoggerMessage(2, LogLevel.Information, "{Type} {Name} stopped")]
    private partial void LogStopped(string type, string name);

    [LoggerMessage(3, LogLevel.Information, "{Type} {Name} is waiting until the system is ready")]
    private partial void LogWaiting(string type, string name);

    #endregion Logging
}

internal interface IAlgoHostGrainInternal : IAlgoHostGrain
{
    Task ApplyOptionsAsync();
}