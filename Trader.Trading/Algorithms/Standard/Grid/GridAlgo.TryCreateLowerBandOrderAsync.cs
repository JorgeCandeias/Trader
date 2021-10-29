using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Exceptions;
using Outcompute.Trader.Trading.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo
    {
        protected async ValueTask<IAlgoCommand?> TryCreateLowerBandOrderAsync(CancellationToken cancellationToken = default)
        {
            // identify the highest and lowest bands
            var highBand = _bands.Max;
            var lowBand = _bands.Min;

            if (lowBand is null || highBand is null)
            {
                _logger.LogError(
                    "{Type} {Name} attempted to create a new lower band without an existing band yet",
                    TypeName, Context.Name);

                // something went wrong so let the algo reset
                return Noop();
            }

            // skip if the current price is at or above the band open price
            if (Context.Ticker.ClosePrice >= lowBand.OpenPrice)
            {
                _logger.LogInformation(
                    "{Type} {Name} reports price {Price} {Quote} is within the current low band of {OpenPrice} {Quote} to {ClosePrice} {Quote}",
                    TypeName, Context.Name, Context.Ticker.ClosePrice, Context.Symbol.QuoteAsset, lowBand.OpenPrice, Context.Symbol.QuoteAsset, lowBand.ClosePrice, Context.Symbol.QuoteAsset);

                // let the algo continue
                return null;
            }

            // skip if we are already at the maximum number of bands
            if (_bands.Count >= _options.MaxBands)
            {
                _logger.LogWarning(
                    "{Type} {Name} has reached the maximum number of {Count} bands",
                    TypeName, Context.Name, _options.MaxBands);

                // let the algo continue
                return null;
            }

            // skip if lower band creation is disabled
            if (!_options.IsLowerBandOpeningEnabled)
            {
                _logger.LogWarning(
                    "{Type} {Name} cannot create lower band because the feature is disabled",
                    TypeName, Context.Name);

                return null;
            }

            // find the lower price under the current price and low band
            var lowerPrice = highBand.OpenPrice;
            var stepPrice = highBand.ClosePrice - highBand.OpenPrice;
            while (lowerPrice >= Context.Ticker.ClosePrice || lowerPrice >= lowBand.OpenPrice)
            {
                lowerPrice -= stepPrice;
            }

            // protect from weird stuff
            if (lowerPrice <= 0)
            {
                throw new AlgorithmException($"Somehow we got to a negative lower price of {lowerPrice}!");
            }

            // under adjust the buy price to the tick size
            lowerPrice = lowerPrice.AdjustPriceUpToTickSize(Context.Symbol);

            // calculate the quote amount to pay with
            var total = GetFreeBalance() * _options.BuyQuoteBalanceFraction;

            // lower below the max notional if needed
            if (_options.MaxNotional.HasValue)
            {
                total = Math.Min(total, _options.MaxNotional.Value);
            }

            // raise to the minimum notional if needed
            total = total.AdjustTotalUpToMinNotional(Context.Symbol);

            // ensure there is enough quote asset for it
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
            var quantity = total / lowerPrice;

            // round it down to the lot size step
            quantity = quantity.AdjustQuantityUpToLotStepSize(Context.Symbol);

            // place the buy order
            var tag = CreateTag(Context.Symbol.Name, lowerPrice);
            return CreateOrder(Context.Symbol, OrderType.Limit, OrderSide.Buy, TimeInForce.GoodTillCanceled, quantity, lowerPrice, tag);
        }
    }
}