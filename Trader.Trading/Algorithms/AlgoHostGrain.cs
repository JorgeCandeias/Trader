using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using System;
using System.Threading;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <inheritdoc cref="IAlgoHostGrain" />
    internal class AlgoHostGrain : Grain, IAlgoHostGrainInternal
    {
        private readonly ILogger _logger;
        private readonly IOptionsMonitor<AlgoHostGrainOptions> _options;
        private readonly IAlgoFactoryResolver _resolver;
        private readonly IGrainFactory _factory;

        public AlgoHostGrain(ILogger<AlgoHostGrain> logger, IOptionsMonitor<AlgoHostGrainOptions> options, IAlgoFactoryResolver resolver, IGrainFactory factory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        private readonly CancellationTokenSource _cancellation = new();

        private string _name = Empty;
        private IAlgo _algo = NullAlgo.Instance;
        private IDisposable? _timer;

        public override async Task OnActivateAsync()
        {
            // the name of the algo is the key for this grain instance
            _name = this.GetPrimaryKeyString();

            // snapshot the current algo host options
            var options = _options.Get(_name);

            // resolve the factory for the current algo type
            var factory = _resolver.Resolve(options.Type);

            // create the algo instance
            _algo = factory.Create(_name);

            // keep the execution behaviour updated upon options change
            _options.OnChange(_ => this.AsReference<IAlgoHostGrainInternal>().InvokeOneWay(x => x.ApplyOptionsAsync()));

            // apply the options now
            await ApplyOptionsAsync().ConfigureAwait(true);

            _logger.AlgoHostGrainStarted(_name);
        }

        public override Task OnDeactivateAsync()
        {
            if (_timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }

            _cancellation.Cancel();

            _logger.AlgoHostGrainStopped(_name);

            return base.OnDeactivateAsync();
        }

        public Task PingAsync() => Task.CompletedTask;

        public Task ApplyOptionsAsync()
        {
            var options = _options.Get(_name);

            // enable or disable timer based execution as needed
            if (options.Enabled && options.TickEnabled)
            {
                if (_timer is null)
                {
                    _timer = RegisterTimer(_ => ExecuteAlgoAsync(), null, options.TickDelay, options.TickDelay);
                }
            }
            else
            {
                if (_timer is not null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }

            return Task.CompletedTask;
        }

        private async Task ExecuteAlgoAsync()
        {
            // snapshot the options for this execution
            var options = _options.Get(_name);

            // enforce the execution time limit along with grain cancellation
            using var limit = new CancellationTokenSource(options.MaxExecutionTime);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(limit.Token, _cancellation.Token);

            // execute the algo under the limits
            await _algo.GoAsync(linked.Token).ConfigureAwait(true);
        }

        public Task TickAsync()
        {
            return ExecuteAlgoAsync();
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
    }
}