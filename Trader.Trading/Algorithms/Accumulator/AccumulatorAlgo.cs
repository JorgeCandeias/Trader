using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Accumulator
{
    internal class AccumulatorAlgo : Algo
    {
        private readonly IAlgoContext _context;
        private readonly IOptionsMonitor<AccumulatorAlgoOptions> _options;
        private readonly ILogger _logger;

        public AccumulatorAlgo(IAlgoContext context, IOptionsMonitor<AccumulatorAlgoOptions> options, ILogger<AccumulatorAlgo> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static string TypeName => nameof(AccumulatorAlgo);

        public override async Task<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
        {
            // snapshot the options for this execution
            var options = _options.Get(_context.Name);

            // get the symbol information from the context
            var symbol = await _context.GetRequiredSymbolAsync(options.Symbol, cancellationToken);

            if (!options.Enabled)
            {
                _logger.LogInformation("{Type} {Name} is disabled", TypeName, _context.Name);

                return Noop();
            }

            _logger.LogInformation("{Type} {Name} running...", TypeName, _context.Name);

            await _context.PublishProfitAsync(Profit.Zero(symbol.Name, symbol.BaseAsset, symbol.QuoteAsset));

            return TrackingBuy(symbol, options.PullbackRatio, options.TargetQuoteBalanceFractionPerBuy, options.MaxNotional, options.RedeemSavings);
        }
    }
}