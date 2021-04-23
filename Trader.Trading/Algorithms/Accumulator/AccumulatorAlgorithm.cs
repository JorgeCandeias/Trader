using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;

namespace Trader.Trading.Algorithms.Accumulator
{
    internal class AccumulatorAlgorithm : IAccumulatorAlgorithm
    {
        private readonly string _name;
        private readonly AccumulatorAlgorithmOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;
        private readonly IOrderSynchronizer _orderSynchronizer;
        private readonly ITraderRepository _repository;

        public AccumulatorAlgorithm(string name, IOptionsSnapshot<AccumulatorAlgorithmOptions> options, ILogger<AccumulatorAlgorithm> logger, ITradingService trader, ISystemClock clock, IOrderSynchronizer orderSynchronizer, ITraderRepository repository)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options.Get(name) ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _orderSynchronizer = orderSynchronizer ?? throw new ArgumentNullException(nameof(orderSynchronizer));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        private static string Type => nameof(AccumulatorAlgorithm);
        public string Symbol => _options.Symbol;

        private SortedOrderSet _orders;

        public async Task GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo, CancellationToken cancellationToken = default)
        {
            await _orderSynchronizer.SynchronizeOrdersAsync(_options.Symbol, cancellationToken);

            await GetOpenOrdersAsync(cancellationToken);

            // get the current price
            var ticker = await _trader.GetSymbolPriceTickerAsync(_options.Symbol, cancellationToken);

            // get the symbol filters
            var symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();

            // get the account free quote balance
            var free = accountInfo.Balances.Single(x => x.Asset == _options.Quote).Free;

            // identify the target low price for the first buy
            var lowBuyPrice = ticker.Price * _options.PullbackRatio;

            // under adjust the buy price to the tick size
            lowBuyPrice = Math.Floor(lowBuyPrice / priceFilter.TickSize) * priceFilter.TickSize;

            _logger.LogInformation(
                "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                Type, _name, lowBuyPrice, _options.Quote, ticker.Price, _options.Quote);

            if (await TryCloseLowBuysAsync(lowBuyPrice, cancellationToken)) return;
            if (await TryCloseHighBuysAsync(cancellationToken)) return;

            // if there are still open orders then leave them be
            if (_orders.Any(x => x.Side == OrderSide.Buy))
            {
                return;
            }

            // calculate the amount to pay with
            var total = Math.Round(Math.Max(free * _options.TargetQuoteBalanceFractionPerBuy, _options.MinQuoteAssetQuantityPerOrder), symbol.QuoteAssetPrecision);

            // ensure there is enough quote asset for it
            if (total > free)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}",
                    Type, _name, total, _options.Quote, free, _options.Quote);

                return;
            }

            // calculate the appropriate quantity to buy
            var quantity = total / lowBuyPrice;

            // round it down to the lot size step
            quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

            // place the order now
            var order = await _trader.CreateOrderAsync(
                new Order(
                    _options.Symbol,
                    OrderSide.Buy,
                    OrderType.Limit,
                    TimeInForce.GoodTillCanceled,
                    quantity,
                    null,
                    lowBuyPrice,
                    $"{lowBuyPrice:N8}".Replace(".", "").Replace(",", ""),
                    null,
                    null,
                    NewOrderResponseType.Full,
                    null,
                    _clock.UtcNow),
                cancellationToken);

            await _repository.ApplyAsync(order, cancellationToken);

            _logger.LogInformation(
                "{Type} {Name} created {OrderSide} {OrderType} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                Type, _name, order.Side, order.Type, order.Symbol, order.OriginalQuantity, _options.Asset, order.Price, _options.Quote, order.OriginalQuantity * order.Price, _options.Quote);
        }

        private async Task GetOpenOrdersAsync(CancellationToken cancellationToken)
        {
            _orders = await _repository.GetTransientOrdersAsync(_options.Symbol, OrderSide.Buy, default, cancellationToken);

            foreach (var order in _orders)
            {
                _logger.LogInformation(
                    "{Type} {Name} identified open {OrderSide} {OrderType} order for {Quantity} {Asset} at {Price} {Quote} totalling {Notional:N8} {Quote}",
                    Type, _name, order.Side, order.Type, order.OriginalQuantity, _options.Asset, order.Price, _options.Quote, order.OriginalQuantity * order.Price, _options.Quote);
            }
        }

        private async Task<bool> TryCloseLowBuysAsync(decimal lowBuyPrice, CancellationToken cancellationToken)
        {
            // cancel all open buy orders with an open price lower than the lower band to the current price
            var closed = false;
            foreach (var order in _orders.Where(x => x.Side == OrderSide.Buy && x.Price < lowBuyPrice))
            {
                var result = await _trader.CancelOrderAsync(
                    new CancelStandardOrder(
                        _options.Symbol,
                        order.OrderId,
                        null,
                        null,
                        null,
                        _clock.UtcNow),
                    cancellationToken);

                await _repository.ApplyAsync(result, cancellationToken);

                _logger.LogInformation(
                    "{Type} {Name} cancelled low starting open order with price {Price} for {Quantity} units",
                    Type, _name, result.Price, result.OriginalQuantity);

                closed = true;
            }

            // let the algo resync if any orders where closed
            return closed;
        }

        private async Task<bool> TryCloseHighBuysAsync(CancellationToken cancellationToken)
        {
            var closed = false;
            foreach (var order in _orders.Where(x => x.Side == OrderSide.Buy).OrderBy(x => x.Price).Skip(1))
            {
                var result = await _trader.CancelOrderAsync(
                    new CancelStandardOrder(
                        _options.Symbol,
                        order.OrderId,
                        null,
                        null,
                        null,
                        _clock.UtcNow),
                    cancellationToken);

                await _repository.ApplyAsync(result, cancellationToken);

                _logger.LogInformation(
                    "{Type} {Name} cancelled low starting open order with price {Price} for {Quantity} units",
                    Type, _name, result.Price, result.OriginalQuantity);

                closed = true;
            }

            // let the algo resync if any orders where closed
            return closed;
        }

        public Task<Profit> GetProfitAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Profit.Zero);
        }

        public Task<Statistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Statistics.Zero);
        }
    }
}