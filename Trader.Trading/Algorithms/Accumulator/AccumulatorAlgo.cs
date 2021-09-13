using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Trading.Algorithms.Exceptions;
using Outcompute.Trader.Trading.Algorithms.Steps;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Accumulator
{
    internal class AccumulatorAlgo : IAlgo
    {
        private readonly IAlgoContext _context;
        private readonly IOptionsMonitor<AccumulatorAlgoOptions> _options;
        private readonly ILogger _logger;
        private readonly ITrackingBuyStep _trackingBuyStep;

        public AccumulatorAlgo(IAlgoContext context, IOptionsMonitor<AccumulatorAlgoOptions> options, ILogger<AccumulatorAlgo> logger, ITrackingBuyStep trackingBuyStep)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // todo: attach this to the algo context via an extension
            _trackingBuyStep = trackingBuyStep ?? throw new ArgumentNullException(nameof(trackingBuyStep));
        }

        private static string TypeName => nameof(AccumulatorAlgo);

        public async Task GoAsync(CancellationToken cancellationToken = default)
        {
            // snapshot the options for this execution
            var options = _options.Get(_context.Name);

            // get the symbol information from the context
            var symbol = await _context
                .TryGetSymbolAsync(options.Symbol)
                .ConfigureAwait(false);

            if (symbol is null)
            {
                throw new AlgorithmNotInitializedException();
            }

            if (!options.Enabled)
            {
                _logger.LogInformation("{Type} {Name} is disabled", TypeName, _context.Name);
                return;
            }

            _logger.LogInformation("{Type} {Name} running...", TypeName, _context.Name);

            await _trackingBuyStep
                .GoAsync(symbol, options.PullbackRatio, options.TargetQuoteBalanceFractionPerBuy, options.MaxNotional, cancellationToken)
                .ConfigureAwait(false);

            await _context
                .PublishProfitAsync(Profit.Zero(symbol.Name, symbol.BaseAsset, symbol.QuoteAsset))
                .ConfigureAwait(false);
        }
    }
}