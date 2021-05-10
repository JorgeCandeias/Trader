using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using Trader.Models;

namespace Trader.Trading.Algorithms.TimeAveraging
{
    internal class TimeAveragingAlgorithm : ITradingAlgorithm
    {
        private readonly string _name;
        private readonly TimeAveragingAlgorithmOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingRepository _repository;
        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;

        public TimeAveragingAlgorithm(string name, IOptionsSnapshot<TimeAveragingAlgorithmOptions> options, ILogger<TimeAveragingAlgorithm> logger, ITradingRepository repository, ISystemClock clock, ITradingService trader)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options.Get(_name) ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
        }

        private Symbol? _symbol;
        private MinNotionalSymbolFilter? _minNotionalFilter;

        private static string Type => nameof(TimeAveragingAlgorithm);
        public string Symbol => _options.Symbol;

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            if (_symbol is null) throw new AlgorithmNotInitializedException();

            return Task.FromResult(Profit.Zero(_symbol.QuoteAsset));
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Statistics.Zero);
        }

        public Task InitializeAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default)
        {
            if (exchangeInfo is null) throw new ArgumentNullException(nameof(exchangeInfo));

            _symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);
            _minNotionalFilter = _symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();

            return Task.CompletedTask;
        }

        public async Task GoAsync(CancellationToken cancellationToken = default)
        {
            if (_symbol is null) throw new AlgorithmNotInitializedException();
            if (_minNotionalFilter is null) throw new AlgorithmNotInitializedException();

            // get the latest buy order
            var order = await _repository
                .GetLatestOrderBySideAsync(_options.Symbol, OrderSide.Buy, cancellationToken)
                .ConfigureAwait(false);

            // calculate the next buy time
            var due = order is null ? DateTime.MinValue : order.UpdateTime.Add(_options.Period);

            // skip if we are not due yet
            var remaining = due.Subtract(_clock.UtcNow);
            if (remaining > TimeSpan.Zero)
            {
                _logger.LogInformation(
                    "{Type} {Symbol} reports the next due order time is at {DueTime} with {Remaining} time to go",
                    Type, Symbol, due, remaining);

                return;
            }

            // get the available balance for the quote asset
            var balance = await _repository
                .GetBalanceAsync(_symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);

            if (balance is null)
            {
                throw new AlgorithmException($"Could not get balance for asset '{_symbol.QuoteAsset}'");
            }

            // calculate the total to take from the balance
            var total = Math.Round(balance.Free * _options.QuoteFractionPerBuy, _symbol.QuoteAssetPrecision);

            // raise the total to the minimum notional if needed
            total = Math.Max(total, _minNotionalFilter.MinNotional);

            // ensure there is enough quote asset for it
            if (total > balance.Free)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}",
                    Type, _name, total, _symbol.QuoteAsset, balance.Free, _symbol.QuoteAsset);

                return;
            }

            var orderType = OrderType.Market;
            var orderSide = OrderSide.Buy;
            var timeInForce = TimeInForce.FillOrKill;

            _logger.LogInformation(
                "{Type} {Symbol} reports the next due time of {DueTime} has been reached and is placing a {OrderType} {OrderSide} {TimeInForce} order for a total of {Notional} {Quote}",
                Type, Symbol, due, orderType, orderSide, timeInForce, total, _symbol.QuoteAsset);

            var result = await _trader
                .CreateOrderAsync(
                    new Order(
                        _options.Symbol,
                        orderSide,
                        orderType,
                        timeInForce,
                        null,
                        total,
                        null,
                        $"{_options.Symbol}{due:yyyyMMddHHmmssfff}",
                        null,
                        null,
                        NewOrderResponseType.Full,
                        null,
                        _clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);

            await _repository
                .SetOrderAsync(result, 0, 0, total, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}