using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using Trader.Models;
using Trader.Trading.Algorithms.Exceptions;
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

        private Symbol? _symbol;

        public string Symbol => _options.Symbol;

        private static string Type => nameof(ValueAveragingAlgorithm);

        private Profit? _profit;

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            if (_symbol is null)
            {
                throw new AlgorithmNotInitializedException();
            }

            return Task.FromResult(_profit ?? Profit.Zero(_symbol.QuoteAsset));
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profit is null ? Statistics.Zero : Statistics.FromProfit(_profit));
        }

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

            _logger.LogInformation("{Type} {Name} running...", Type, _name);

            // run the resolve to calculate profit
            var result = await _significantOrderResolver
                .ResolveAsync(_symbol, cancellationToken)
                .ConfigureAwait(false);

            _profit = result.Profit;

            if ((result.Orders.Count > 0 && _options.IsAveragingEnabled) ||
                (result.Orders.Count == 0 && _options.IsOpeningEnabled))
            {
                await _trackingBuyStep
                    .GoAsync(_symbol, _options.PullbackRatio, _options.TargetQuoteBalanceFractionPerBuy, _options.MaxNotional, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                // cancel all open buys
                var orders = await _repository
                    .GetTransientOrdersBySideAsync(_symbol.Name, OrderSide.Buy, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var order in orders)
                {
                    await _trader
                        .CancelOrderAsync(new CancelStandardOrder(_options.Symbol, order.OrderId, null, null, null, _clock.UtcNow), cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            // then place the averaging sell
            await _averagingSellStep
                .GoAsync(_symbol, _options.ProfitMultipler, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}