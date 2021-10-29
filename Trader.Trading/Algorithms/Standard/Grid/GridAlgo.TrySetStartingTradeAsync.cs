using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo
    {
        protected async ValueTask<IAlgoCommand?> TrySetStartingTradeAsync(CancellationToken cancellationToken = default)
        {
            // stop opening evaluation if it is disabled
            if (!_options.IsOpeningEnabled)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create the opening band because it is disabled",
                    TypeName, Context.Name);

                return ClearOpenOrders(OrderSide.Buy);
            }

            // skip if there is more than one band
            if (_bands.Count > 1)
            {
                return null;
            }

            // skip if there is one band but it is already active
            if (_bands.Count == 1 && _bands.Min!.Status != BandStatus.Ordered)
            {
                return null;
            }

            // identify the target low price for the first buy
            var lowBuyPrice = Context.Ticker.ClosePrice;

            // under adjust the buy price to the tick size
            lowBuyPrice = lowBuyPrice.AdjustPriceDownToTickSize(Context.Symbol);

            _logger.LogInformation(
                "{Type} {Name} identified first buy target price at {LowPrice} {LowQuote} with current price at {CurrentPrice} {CurrentQuote}",
                TypeName, Context.Name, lowBuyPrice, Context.Symbol.QuoteAsset, Context.Ticker.ClosePrice, Context.Symbol.QuoteAsset);

            // cancel the lowest open buy order with a open price lower than the lower band to the current price
            var lowest = Context.Orders.FirstOrDefault(x => x.Side == OrderSide.Buy && x.Status.IsTransientStatus() && x.Price < lowBuyPrice);
            if (lowest is not null)
            {
                return CancelOrder(Context.Symbol, lowest.OrderId);
            }

            // calculate the amount to pay with
            var total = Math.Round(GetFreeBalance() * _options.BuyQuoteBalanceFraction, Context.Symbol.QuoteAssetPrecision);

            // lower below the max notional if needed
            if (_options.MaxNotional.HasValue)
            {
                total = Math.Min(total, _options.MaxNotional.Value);
            }

            // raise to the minimum notional if needed
            total = total.AdjustTotalUpToMinNotional(Context.Symbol);

            // ensure there is enough quote spot balance for it
            if (total > Context.QuoteSpotBalance.Free)
            {
                var necessary = total - Context.QuoteSpotBalance.Free;

                _logger.LogWarning(
                    "{Type} {Name} cannot create order with amount of {Total} {Quote} because the free amount is only {Free} {Quote}. Will attempt to redeem from savings...",
                    TypeName, Context.Name, total, Context.Symbol.QuoteAsset, Context.QuoteSpotBalance.Free, Context.Symbol.QuoteAsset);

                var result = await TryRedeemSavings(Context.Symbol.QuoteAsset, necessary)
                    .ExecuteAsync(Context, cancellationToken)
                    .ConfigureAwait(false);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "{Type} {Name} redeemed {Amount} {Asset} successfully",
                        TypeName, Context.Name, necessary, Context.Symbol.QuoteAsset);

                    // let the algo cycle to allow redemption to process
                    return Noop();
                }
                else
                {
                    _logger.LogError(
                        "{Type} {Name} failed to redeem the necessary amount of {Quantity} {Asset}",
                        TypeName, Context.Name, necessary, Context.Symbol.QuoteAsset);

                    return null;
                }
            }

            // calculate the appropriate quantity to buy
            var quantity = total / lowBuyPrice;

            // adjust the quantity up to the lot size
            quantity = quantity.AdjustQuantityUpToLotStepSize(Context.Symbol);

            // place a limit order at the current price
            var tag = CreateTag(Context.Symbol.Name, lowBuyPrice);
            return CreateOrder(Context.Symbol, OrderType.Limit, OrderSide.Buy, TimeInForce.GoodTillCanceled, quantity, lowBuyPrice, tag);
        }
    }
}