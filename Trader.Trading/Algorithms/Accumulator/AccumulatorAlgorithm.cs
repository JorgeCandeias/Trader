using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using Trader.Models;
using Trader.Trading.Algorithms.Steps;

namespace Trader.Trading.Algorithms.Accumulator
{
    internal class AccumulatorAlgorithm : IAccumulatorAlgorithm
    {
        private readonly string _name;
        private readonly AccumulatorAlgorithmOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;
        private readonly ITraderRepository _repository;
        private readonly ITrackingBuyStep _trackingBuyStep;

        public AccumulatorAlgorithm(string name, IOptionsSnapshot<AccumulatorAlgorithmOptions> options, ILogger<AccumulatorAlgorithm> logger, ITradingService trader, ISystemClock clock, ITraderRepository repository, ITrackingBuyStep trackingBuyStep)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options.Get(name) ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trackingBuyStep = trackingBuyStep ?? throw new ArgumentNullException(nameof(trackingBuyStep));
        }

        private static string Type => nameof(AccumulatorAlgorithm);

        public string Symbol => _options.Symbol;

        public Task GoAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default)
        {
            if (exchangeInfo is null) throw new ArgumentNullException(nameof(exchangeInfo));

            return GoInnerAsync(exchangeInfo, cancellationToken);
        }

        private async Task GoInnerAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "{Type} {Name} running...",
                Type, _name);

            // get the symbol information
            var symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);

            await _trackingBuyStep
                .GoAsync(symbol, _options.PullbackRatio, _options.TargetQuoteBalanceFractionPerBuy, cancellationToken)
                .ConfigureAwait(false);
        }

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Profit.Zero(_options.Quote));
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Statistics.Zero);
        }
    }
}