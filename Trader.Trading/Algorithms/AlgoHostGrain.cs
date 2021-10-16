using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Outcompute.Trader.Trading.Algorithms.Exceptions;
using Outcompute.Trader.Trading.Readyness;
using System;
using System.Threading;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <inheritdoc cref="IAlgoHostGrain" />
    internal sealed class AlgoHostGrain : Grain, IAlgoHostGrainInternal, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IOptionsMonitor<AlgoHostGrainOptions> _options;
        private readonly IReadynessProvider _readyness;
        private readonly IServiceScope _scope;

        public AlgoHostGrain(ILogger<AlgoHostGrain> logger, IOptionsMonitor<AlgoHostGrainOptions> options, IReadynessProvider readyness, IServiceProvider provider)
        {
            _logger = logger;
            _options = options;
            _readyness = readyness;
            _scope = provider.CreateScope();
        }

        private readonly CancellationTokenSource _cancellation = new();

        private string _name = Empty;
        private IAlgo _algo = NullAlgo.Instance;
        private IDisposable? _timer;
        private bool _ready;
        private bool _loggedNotReady;

        public override async Task OnActivateAsync()
        {
            // the name of the algo is the key for this grain instance
            _name = this.GetPrimaryKeyString();

            // snapshot the current algo host options
            var options = _options.Get(_name);

            // resolve the factory for the current algo type
            var factory = _scope.ServiceProvider.GetRequiredService<IAlgoFactoryResolver>().Resolve(options.Type);

            // create the algo instance
            _algo = factory.Create(_name);

            // perform specific tasks for symbol algos
            // this ad-hoc code needs to get refactored into its own component for each algo type using some strategy pattern
            if (_algo is ISymbolAlgo symbolAlgo)
            {
                // validate the symbol option
                if (IsNullOrWhiteSpace(options.Symbol))
                {
                    throw new AlgorithmException($"Algo '{_name}' is a '{nameof(ISymbolAlgo)}' and must specify the '{nameof(options.Symbol)}' option");
                }

                // get the scoped context
                var context = _scope.ServiceProvider.GetRequiredService<AlgoContext>();

                // set the symbol info on the scoped context
                context.Symbol = await context.GetRequiredSymbolAsync(options.Symbol, _cancellation.Token);
            }

            // run startup work
            await _algo.StartAsync(_cancellation.Token);

            // keep the execution behaviour updated upon options change
            _options.OnChange(_ => this.AsReference<IAlgoHostGrainInternal>().InvokeOneWay(x => x.ApplyOptionsAsync()));

            // apply the options now
            await ApplyOptionsAsync();

            // spin up the readyness check
            RegisterTimer(_ => TickReadynessAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            _logger.AlgoHostGrainStarted(_name);
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
                _logger.AlgoHostGrainStopped(_name);
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

        private void StartTicking(AlgoHostGrainOptions options)
        {
            if (_timer is null)
            {
                _timer = RegisterTimer(_ => ExecuteAlgoAsync(), null, options.TickDelay, options.TickDelay);
            }
        }

        private void StopTicking()
        {
            if (_timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private async Task ExecuteAlgoAsync()
        {
            // skip if the system is not ready
            if (!_ready)
            {
                if (!_loggedNotReady)
                {
                    _logger.AlgoHostGrainSystemNotReady(_name);
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
            await _algo.GoAsync(linked.Token);
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
            _timer?.Dispose();
            _scope.Dispose();
        }
    }

    internal interface IAlgoHostGrainInternal : IAlgoHostGrain
    {
        Task ApplyOptionsAsync();
    }

    internal static class AlgoHostGrainLoggerExtensions
    {
        private static readonly Action<ILogger, string, string, Exception> _started = LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(0, nameof(AlgoHostGrainStarted)),
            "{Grain} {Name} started");

        public static void AlgoHostGrainStarted(this ILogger logger, string name)
        {
            _started(logger, nameof(AlgoHostGrain), name, null!);
        }

        private static readonly Action<ILogger, string, string, Exception> _stopped = LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(0, nameof(AlgoHostGrainStopped)),
            "{Grain} {Name} stopped");

        public static void AlgoHostGrainStopped(this ILogger logger, string name)
        {
            _stopped(logger, nameof(AlgoHostGrain), name, null!);
        }

        private static readonly Action<ILogger, string, string, Exception> _notReady = LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(0, nameof(AlgoHostGrainSystemNotReady)),
            "{Grain} {Name} is waiting until the system is ready...");

        public static void AlgoHostGrainSystemNotReady(this ILogger logger, string name)
        {
            _notReady(logger, nameof(AlgoHostGrain), name, null!);
        }
    }
}