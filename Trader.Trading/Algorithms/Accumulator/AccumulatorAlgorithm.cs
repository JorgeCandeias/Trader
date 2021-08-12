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
    internal class AccumulatorAlgorithm : ITradingAlgorithm
    {
        private readonly string _name;
        private readonly AccumulatorAlgorithmOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;
        private readonly ITradingRepository _repository;
        private readonly ITrackingBuyStep _trackingBuyStep;

        public AccumulatorAlgorithm(string name, IOptionsSnapshot<AccumulatorAlgorithmOptions> options, ILogger<AccumulatorAlgorithm> logger, ITradingService trader, ISystemClock clock, ITradingRepository repository, ITrackingBuyStep trackingBuyStep)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options.Get(name) ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trackingBuyStep = trackingBuyStep ?? throw new ArgumentNullException(nameof(trackingBuyStep));
        }

        private Symbol? _symbol;

        private static string Type => nameof(AccumulatorAlgorithm);

        public string Symbol => _options.Symbol;

        public Task InitializeAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default)
        {
            if (exchangeInfo is null) throw new ArgumentNullException(nameof(exchangeInfo));

            _symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);

            return Task.CompletedTask;
        }

        public async Task GoAsync(CancellationToken cancellationToken = default)
        {
            if (_symbol is null)
            {
                throw new AlgorithmNotInitializedException();
            }

            _logger.LogInformation(
                "{Type} {Name} running...",
                Type, _name);

            await _trackingBuyStep
                .GoAsync(_symbol, _options.PullbackRatio, _options.TargetQuoteBalanceFractionPerBuy, _options.MaxNotional, cancellationToken)
                .ConfigureAwait(false);
        }

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            _ = _symbol ?? throw new AlgorithmNotInitializedException();

            return Task.FromResult(Profit.Zero(_symbol.QuoteAsset));
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Statistics.Zero);
        }
    }
}