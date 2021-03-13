using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;

namespace Trader.Core.Trading.Algorithms.Accumulator
{
    internal class AccumulatorAlgorithm : IAccumulatorAlgorithm
    {
        private readonly string _name;
        private readonly AccumulatorAlgorithmOptions _options;
        private readonly ILogger _logger;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;

        public AccumulatorAlgorithm(string name, IOptionsSnapshot<AccumulatorAlgorithmOptions> options, ILogger<AccumulatorAlgorithm> logger, ITradingService trader, ISystemClock clock)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _options = options.Get(name) ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Type => nameof(AccumulatorAlgorithm);

        public string Symbol => _options.Symbol;

        public async Task GoAsync(ExchangeInfo exchangeInfo, AccountInfo accountInfo, CancellationToken cancellationToken = default)
        {
            // get the orders for this symbol
            var orders = await _trader.GetOpenOrdersAsync(new GetOpenOrders(_options.Symbol, null, _clock.UtcNow), cancellationToken);

            // get the current price
            var ticker = await _trader.GetSymbolPriceTickerAsync(_options.Symbol, cancellationToken);

            // get the symbol filters
            var symbol = exchangeInfo.Symbols.Single(x => x.Name == _options.Symbol);
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();

            // get the account free quote balance
            var free = accountInfo.Balances.Single(x => x.Asset == _options.Quote).Free;

            // identify the target low price for the first buy
            var lowBuyPrice = ticker.Price * _options.PullbackRatio;

            // under adjust the buy price to the tick size
            lowBuyPrice = Math.Floor(lowBuyPrice / priceFilter.TickSize) * priceFilter.TickSize;

            _logger.LogInformation(
                "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                Type, _name, lowBuyPrice, _options.Quote, ticker.Price, _options.Quote);

            // cancel all open buy orders with an open price lower than the lower band to the current price
            var lower = orders.Where(x => x.Side == OrderSide.Buy && x.Price < lowBuyPrice).FirstOrDefault();
            if (lower is not null)
            {
                var result = await _trader.CancelOrderAsync(new CancelStandardOrder(_options.Symbol, lower.OrderId, null, null, null, _clock.UtcNow), cancellationToken);

                _logger.LogInformation(
                    "{Type} {Name} cancelled low starting open order with price {Price} for {Quantity} units",
                    Type, _name, result.Price, result.OriginalQuantity);

                return;
            }

            // if there are still open orders then leave them be
            if (orders.Any(x => x.Side == OrderSide.Buy))
            {
                return;
            }

            // put the starting order through

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
            var order = await _trader.CreateOrderAsync(new Order(
                _options.Symbol,
                OrderSide.Buy,
                OrderType.Limit,
                TimeInForce.GoodTillCanceled,
                quantity,
                null,
                lowBuyPrice,
                null,
                null,
                null,
                NewOrderResponseType.Full,
                null,
                _clock.UtcNow),
                cancellationToken);

            _logger.LogInformation(
                "{Type} {Name} created {OrderSide} {OrderType} order on symbol {Symbol} for {Quantity} {Asset} at price {Price} {Quote} for a total of {Total} {Quote}",
                Type, _name, order.Side, order.Type, order.Symbol, order.OriginalQuantity, _options.Asset, order.Price, _options.Quote, order.OriginalQuantity * order.Price, _options.Quote);
        }

        public IEnumerable<AccountTrade> GetTrades()
        {
            return ImmutableList<AccountTrade>.Empty;
        }
    }
}