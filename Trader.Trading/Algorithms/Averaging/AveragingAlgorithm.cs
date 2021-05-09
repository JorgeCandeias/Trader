using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using Trader.Models;

namespace Trader.Trading.Algorithms.Averaging
{
    internal class AveragingAlgorithm : ITradingAlgorithm
    {
        private readonly string _name;
        private readonly AveragingAlgorithmOptions _options;
        private readonly ILogger _logger;
        private readonly ITraderRepository _repository;
        private readonly ISystemClock _clock;
        private readonly ITradingService _trader;

        public AveragingAlgorithm(string name, IOptionsSnapshot<AveragingAlgorithmOptions> options, ILogger<AveragingAlgorithm> logger, ITraderRepository repository, ISystemClock clock, ITradingService trader)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options.Get(_name) ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
        }

        private static string Type => nameof(AveragingAlgorithm);
        public string Symbol => _options.Symbol;

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Profit.Zero(_options.Quote));
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Statistics.Zero);
        }

        public async Task GoAsync(ExchangeInfo exchangeInfo, CancellationToken cancellationToken = default)
        {
            // get the necessary info from the exchange
            var symbolInfo = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);
            var minNotionalFilter = symbolInfo.Filters.OfType<MinNotionalSymbolFilter>().Single();

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
                .GetBalanceAsync(_options.Quote, cancellationToken)
                .ConfigureAwait(false);

            if (balance is null)
            {
                throw new AlgorithmException($"Could not get balance for asset '{_options.Quote}'");
            }

            // calculate the total to take from the balance
            var total = Math.Round(balance.Free * _options.QuoteFractionPerBuy, symbolInfo.QuoteAssetPrecision);

            // raise the total to the minimum notional if needed
            total = Math.Max(total, minNotionalFilter.MinNotional);

            // ensure there is enough quote asset for it
            if (total > balance.Free)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}",
                    Type, _name, total, _options.Quote, balance.Free, _options.Quote);

                return;
            }

            var orderType = OrderType.Market;
            var orderSide = OrderSide.Buy;
            var timeInForce = TimeInForce.FillOrKill;

            _logger.LogInformation(
                "{Type} {Symbol} reports the next due time of {DueTime} has been reached and is placing a {OrderType} {OrderSide} {TimeInForce} order for a total of {Notional} {Quote}",
                Type, Symbol, due, orderType, orderSide, timeInForce, total, _options.Quote);

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