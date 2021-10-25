﻿using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands.ClearOpenOrders;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Commands.AveragingSell
{
    internal class AveragingSellExecutor : IAlgoCommandExecutor<AveragingSellCommand>
    {
        private readonly ILogger _logger;
        private readonly IBalanceProvider _balances;
        private readonly ISavingsProvider _savings;
        private readonly ITickerProvider _tickers;

        public AveragingSellExecutor(ILogger<AveragingSellExecutor> logger, IBalanceProvider balances, ISavingsProvider savings, ITickerProvider tickers)
        {
            _logger = logger;
            _balances = balances;
            _savings = savings;
            _tickers = tickers;
        }

        private static string TypeName => nameof(AveragingSellExecutor);

        public async Task ExecuteAsync(IAlgoContext context, AveragingSellCommand result, CancellationToken cancellationToken = default)
        {
            // get required data
            var balance = await _balances.GetBalanceOrZeroAsync(result.Symbol.BaseAsset, cancellationToken).ConfigureAwait(false);
            var savings = await _savings.GetPositionOrZeroAsync(result.Symbol.BaseAsset, cancellationToken).ConfigureAwait(false);
            var ticker = await _tickers.GetRequiredTickerAsync(result.Symbol.Name, cancellationToken).ConfigureAwait(false);

            // calculate the desired sell
            var desired = CalculateDesiredSell(result.Symbol, result.ProfitMultiplier, result.Orders, balance, savings, ticker);

            // apply the desired sell
            if (desired == DesiredSell.None)
            {
                await new ClearOpenOrdersCommand(result.Symbol, OrderSide.Sell)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await new EnsureSingleOrderCommand(result.Symbol, OrderSide.Sell, OrderType.Limit, TimeInForce.GoodTillCanceled, desired.Quantity, desired.Price, result.RedeemSavings)
                    .ExecuteAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private DesiredSell CalculateDesiredSell(Symbol symbol, decimal profitMultiplier, IReadOnlyCollection<OrderQueryResult> orders, Balance balance, SavingsPosition savings, MiniTicker ticker)
        {
            // skip if there is nothing to sell
            if (orders.Count == 0)
            {
                return DesiredSell.None;
            }

            // take all known significant buy orders on the symbol
            var quantity = orders.Sum(x => x.ExecutedQuantity);

            // break if there are no assets to sell
            var total = balance.Free + savings.FreeAmount;
            if (total < quantity)
            {
                _logger.LogWarning(
                    "{Type} cannot evaluate desired sell for symbol {Symbol} because there are not enough assets available to sell",
                    TypeName, symbol.Name);

                return DesiredSell.None;
            }

            // calculate the weighted average price on all the significant orders
            var price = orders.Sum(x => x.Price * x.ExecutedQuantity) / quantity;

            // bump the price by the profit multipler so we have a sell price
            price *= profitMultiplier;

            // adjust the quantity down to lot size filter
            if (quantity < symbol.Filters.LotSize.StepSize)
            {
                _logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} because the quantity is under the minimum lot size of {MinLotSize} {Asset}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, symbol.Filters.LotSize.StepSize, symbol.BaseAsset);

                return DesiredSell.None;
            }
            quantity = Math.Floor(quantity / symbol.Filters.LotSize.StepSize) * symbol.Filters.LotSize.StepSize;

            // adjust the sell price up to the minimum percent filter
            var minPrice = ticker.ClosePrice * symbol.Filters.PercentPrice.MultiplierDown;
            if (price < minPrice)
            {
                price = minPrice;
            }

            // adjust the sell price up to the tick size
            price = Math.Ceiling(price / symbol.Filters.Price.TickSize) * symbol.Filters.Price.TickSize;

            // check if the sell is under the minimum notional filter
            if (quantity * price < symbol.Filters.MinNotional.MinNotional)
            {
                _logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the minimum notional of {MinNotional} {Quote}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, symbol.Filters.MinNotional.MinNotional, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // check if the sell is above the maximum percent filter
            if (price > ticker.ClosePrice * symbol.Filters.PercentPrice.MultiplierUp)
            {
                _logger.LogError(
                    "{Type} {Name} cannot set sell order for {Quantity} {Asset} at {Price} {Quote} totalling {Total} {Quote} because it is under the maximum percent filter price of {MaxPrice} {Quote}",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, quantity * price, symbol.QuoteAsset, ticker.ClosePrice * symbol.Filters.PercentPrice.MultiplierUp, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // only sell if the price is at or above the ticker
            if (ticker.ClosePrice < price)
            {
                _logger.LogInformation(
                    "{Type} {Name} holding off sell order of {Quantity} {Asset} until price hits {Price} {Quote} ({Percent:P2} of current value of {Ticker} {Quote})",
                    TypeName, symbol.Name, quantity, symbol.BaseAsset, price, symbol.QuoteAsset, price / ticker.ClosePrice, ticker.ClosePrice, symbol.QuoteAsset);

                return DesiredSell.None;
            }

            // otherwise we now have a valid desired sell
            return new DesiredSell(quantity, price);
        }

        private sealed record DesiredSell(decimal Quantity, decimal Price)
        {
            public static readonly DesiredSell None = new(0m, 0m);
        }
    }
}