using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using System;
using System.Threading.Tasks;
using static System.String;

namespace Outcompute.Trader.Trading.Algorithms
{
    /// <inheritdoc cref="IAlgoHostGrain" />
    internal class AlgoHostGrain : Grain, IAlgoHostGrain
    {
        private readonly ILogger _logger;
        private readonly IOptionsMonitor<AlgoHostGrainOptions> _options;
        private readonly IAlgoFactoryResolver _resolver;

        public AlgoHostGrain(ILogger<AlgoHostGrain> logger, IOptionsMonitor<AlgoHostGrainOptions> options, IAlgoFactoryResolver resolver)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        private string _name = Empty;
        private IAlgo _algo = NullAlgo.Instance;

        public override Task OnActivateAsync()
        {
            // the name of the algo is the key for this grain instance
            _name = this.GetPrimaryKeyString();

            // snapshot the current algo host options
            var options = _options.Get(_name);

            // resolve the factory for the current algo type
            var factory = _resolver.Resolve(options.Type);

            // create the algo instance
            _algo = factory.Create(_name);

            _logger.AlgoHostGrainStarted(_name);

            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            _logger.AlgoHostGrainStopped(_name);

            return base.OnDeactivateAsync();
        }

        public Task PingAsync() => Task.CompletedTask;
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