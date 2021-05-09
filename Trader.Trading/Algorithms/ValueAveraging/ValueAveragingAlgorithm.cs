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

namespace Trader.Trading.Algorithms.ValueAveraging
{
    internal class ValueAveragingAlgorithm : ITradingAlgorithm
    {
        private readonly string _name;
        private readonly ValueAveragingAlgorithmOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISignificantOrderResolver _significantOrderResolver;
        private readonly ISystemClock _clock;
        private readonly ITrackingBuyStep _trackingBuyStep;
        private readonly IAveragingSellStep _averagingSellStep;

        public ValueAveragingAlgorithm(string name, IOptionsSnapshot<ValueAveragingAlgorithmOptions> options, ILogger<ValueAveragingAlgorithmOptions> logger, ITradingRepository repository, ITradingService trader, ISignificantOrderResolver significantOrderResolver, ISystemClock clock, ITrackingBuyStep trackingBuyStep, IAveragingSellStep averagingSellStep)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options?.Get(_name) ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _significantOrderResolver = significantOrderResolver ?? throw new ArgumentNullException(nameof(significantOrderResolver));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trackingBuyStep = trackingBuyStep ?? throw new ArgumentNullException(nameof(trackingBuyStep));
            _averagingSellStep = averagingSellStep ?? throw new ArgumentNullException(nameof(averagingSellStep));
        }

        public string Symbol => _options.Symbol;

        private static string Type => nameof(ValueAveragingAlgorithm);

        private Profit? _profit;

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profit ?? Profit.Zero(_options.Quote));
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profit is null ? Statistics.Zero : Statistics.FromProfit(_profit));
        }

        public async Task GoAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("{Type} {Name} running...", Type, _name);

            // grab the symbol information
            // todo: make this a dictionary up front so the algos dont have to enumerate it all the time
            var symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);

            // run the resolve to calculate profit
            var result = await _significantOrderResolver
                .ResolveAsync(symbol.Name, symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);

            _profit = result.Profit;

            // first place the tracking buy
            if (_options.IsOpeningEnabled)
            {
                if (await _trackingBuyStep
                    .GoAsync(symbol, _options.PullbackRatio, _options.TargetQuoteBalanceFractionPerBuy, cancellationToken)
                    .ConfigureAwait(false))
                {
                    return;
                }
            }
            else
            {
                // if there are no significant orders left to sell then stop averaging them
                if (result.Orders.Count == 0)
                {
                    return;
                }

                // otherwise keep averaging as normal
                if (await _trackingBuyStep
                    .GoAsync(symbol, _options.PullbackRatio, _options.TargetQuoteBalanceFractionPerBuy, cancellationToken)
                    .ConfigureAwait(false))
                {
                    return;
                }
            }

            // then place the averaging sell
            await _averagingSellStep
                .GoAsync(symbol, _options.ProfitMultipler, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}