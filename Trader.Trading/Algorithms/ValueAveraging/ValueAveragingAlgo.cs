using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using Outcompute.Trader.Data;
using Outcompute.Trader.Trading.Algorithms.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.ValueAveraging
{
    internal class ValueAveragingAlgo : IAlgo
    {
        private readonly IAlgoContext _context;
        private readonly IOptionsMonitor<ValueAveragingAlgoOptions> _options;
        private readonly ILogger _logger;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISignificantOrderResolver _significantOrderResolver;
        private readonly ISystemClock _clock;

        public ValueAveragingAlgo(IAlgoContext context, IOptionsMonitor<ValueAveragingAlgoOptions> options, ILogger<ValueAveragingAlgoOptions> logger, ITradingRepository repository, ITradingService trader, ISignificantOrderResolver significantOrderResolver, ISystemClock clock)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _significantOrderResolver = significantOrderResolver ?? throw new ArgumentNullException(nameof(significantOrderResolver));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string TypeName => nameof(ValueAveragingAlgo);

        public async Task GoAsync(CancellationToken cancellationToken = default)
        {
            var options = _options.Get(_context.Name);

            var symbol = await _context.TryGetSymbolAsync(options.Symbol).ConfigureAwait(false)
                ?? throw new AlgorithmNotInitializedException();

            _logger.LogInformation("{Type} {Name} running...", TypeName, _context.Name);

            // run the resolve to calculate profit
            var result = await _significantOrderResolver
                .ResolveAsync(symbol, cancellationToken)
                .ConfigureAwait(false);

            if ((result.Orders.Count > 0 && options.IsAveragingEnabled) ||
                (result.Orders.Count == 0 && options.IsOpeningEnabled))
            {
                await _context
                    .SetTrackingBuyAsync(symbol, options.PullbackRatio, options.TargetQuoteBalanceFractionPerBuy, options.MaxNotional, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                // cancel all open buys
                await _context.ClearOpenBuyOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);
            }

            // then place the averaging sell
            await _context
                .SetAveragingSellAsync(symbol, options.ProfitMultipler, options.UseSavings, cancellationToken)
                .ConfigureAwait(false);

            // publish the profit stats
            await _context
                .PublishProfitAsync(result.Profit)
                .ConfigureAwait(false);
        }
    }
}