using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Data;
using Trader.Models;

namespace Trader.Trading.Algorithms.Steps
{
    internal class AveragingSellStep : IAveragingSellStep
    {
        private readonly ILogger _logger;
        private readonly ISignificantOrderResolver _significantOrderResolver;
        private readonly ITraderRepository _repository;
        private readonly ITradingService _trader;
        private readonly ISystemClock _clock;

        public AveragingSellStep(ILogger<AveragingSellStep> logger, ISignificantOrderResolver significantOrderResolver, ITraderRepository repository, ITradingService trader, ISystemClock clock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _significantOrderResolver = significantOrderResolver ?? throw new ArgumentNullException(nameof(significantOrderResolver));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trader = trader ?? throw new ArgumentNullException(nameof(trader));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Type => nameof(AveragingSellStep);

        public Task GoAsync(Symbol symbol, decimal profitMultiplier, CancellationToken cancellationToken = default)
        {
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return GoInnerAsync(symbol, profitMultiplier, cancellationToken);
        }

        private async Task<Profit> GoInnerAsync(Symbol symbol, decimal profitMultiplier, CancellationToken cancellationToken)
        {
            // get any required filters from the symbol
            var priceFilter = symbol.Filters.OfType<PriceSymbolFilter>().Single();
            var percentFilter = symbol.Filters.OfType<PercentPriceSymbolFilter>().Single();
            var minNotionalFilter = symbol.Filters.OfType<MinNotionalSymbolFilter>().Single();
            var lotSizeFilter = symbol.Filters.OfType<LotSizeSymbolFilter>().Single();

            // get the current ticker for the symbol
            var ticker = await _repository
                .GetTickerAsync(symbol.Name, cancellationToken)
                .ConfigureAwait(false);

            // get all significant buys
            var significant = await _significantOrderResolver
                .ResolveAsync(symbol.Name, symbol.QuoteAsset, cancellationToken)
                .ConfigureAwait(false);

            // skip if there is nothing to sell
            if (significant.Orders.IsEmpty)
            {
                return significant.Profit;
            }

            // take all known significant buy orders on the symbol
            var quantity = significant.Orders.Sum(x => x.ExecutedQuantity);

            // calculate the weighted average price on all the significant orders
            var price = significant.Orders.Sum(x => x.Price * x.ExecutedQuantity) / quantity;

            // bump the price by the profit multipler so we have a sell price
            price *= profitMultiplier;

            // adjust the quantity down to lot size filter
            quantity = Math.Floor(quantity / lotSizeFilter.StepSize) * lotSizeFilter.StepSize;

            // adjust the sell price up to the minimum percent filter
            var minPrice = ticker.ClosePrice * percentFilter.MultiplierDown;
            if (price < minPrice)
            {
                price = minPrice;
            }

            // adjust the sell price up to the tick size
            price = Math.Ceiling(price / priceFilter.TickSize) * priceFilter.TickSize;

            // check if the sell is under the minimum notional filter
            if (quantity * price < minNotionalFilter.MinNotional)
            {
                _logger.LogInformation(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the minimum notional of {MinNotional} {Quote}",
                    Type, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, minNotionalFilter.MinNotional, symbol.QuoteAsset);

                return significant.Profit;
            }

            // we now have a valid desired sell
            var desired = new DesiredSell(quantity, price);

            // remove all non-desired buy orders and set the desired sell order if needed
            await SetDesiredStateAsync(symbol, desired, cancellationToken).ConfigureAwait(false);

            // return the latest known profit
            return significant.Profit;
        }

        private async Task SetDesiredStateAsync(Symbol symbol, DesiredSell desired, CancellationToken cancellationToken)
        {
            var orders = await _repository
                .GetTransientOrdersBySideAsync(symbol.Name, OrderSide.Sell, cancellationToken)
                .ConfigureAwait(false);

            foreach (var order in orders)
            {
                if (order.Type != OrderType.Limit || order.OriginalQuantity != desired.Quantity || order.Price != desired.Price)
                {
                    _logger.LogInformation(
                        "{Type} {Name} cancelling non-desired {OrderType} {OrderSide} order {OrderId} for {Quantity} {Asset} at {Price} {Quote}",
                        Type, symbol.Name, order.Type, order.Side, order.OrderId, order.OriginalQuantity, symbol.BaseAsset, order.Price, symbol.QuoteAsset);

                    var result = await _trader
                        .CancelOrderAsync(
                            new CancelStandardOrder(
                                order.Symbol,
                                order.OrderId,
                                null,
                                null,
                                null,
                                _clock.UtcNow),
                            cancellationToken)
                        .ConfigureAwait(false);

                    await _repository
                        .SetOrderAsync(result, cancellationToken)
                        .ConfigureAwait(false);

                    // let the balances resync now
                    return;
                }
            }

            // if there is no order left then we can set the desired sell
            if (orders.IsEmpty)
            {
                var orderType = OrderType.Limit;
                var orderSide = OrderSide.Sell;

                _logger.LogInformation(
                    "{Type} {Name} placing {OrderType} {OrderSide} order for {Quantity} {Asset} at {Price} {Quote}",
                    Type, symbol.Name, orderType, orderSide, desired.Quantity, symbol.BaseAsset, desired.Price, symbol.QuoteAsset);

                var result = await _trader
                    .CreateOrderAsync(
                        new Order(
                            symbol.Name,
                            orderSide,
                            orderType,
                            TimeInForce.GoodTillCanceled,
                            desired.Quantity,
                            null,
                            desired.Price,
                            $"{symbol.Name}{desired.Price:F8}".Replace(".", "", StringComparison.Ordinal),
                            null,
                            null,
                            NewOrderResponseType.Full,
                            null,
                            _clock.UtcNow),
                        cancellationToken)
                    .ConfigureAwait(false);

                await _repository
                    .SetOrderAsync(result, 0m, 0m, 0m, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private record DesiredSell(decimal Quantity, decimal Price);
    }
}