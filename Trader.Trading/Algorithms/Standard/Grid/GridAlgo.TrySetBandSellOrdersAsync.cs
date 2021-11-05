using Microsoft.Extensions.Logging;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Commands;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Algorithms.Standard.Grid
{
    internal partial class GridAlgo
    {
        /// <summary>
        /// Sets sell orders for open bands that do not have them yet.
        /// </summary>
        protected async ValueTask<IAlgoCommand?> TrySetBandSellOrdersAsync(CancellationToken cancellationToken = default)
        {
            // skip if we have reach the max sell orders
            if (_transient.Where(x => x.Side == OrderSide.Sell).Take(_options.MaxActiveSellOrders).Count() >= _options.MaxActiveSellOrders)
            {
                return null;
            }

            // create a sell order for the lowest band only
            foreach (var band in _bands.Where(x => x.Status == BandStatus.Open).Take(_options.MaxActiveSellOrders))
            {
                if (band.CloseOrderId is 0)
                {
                    // acount for leftovers
                    if (band.Quantity > Context.AssetSpotBalance.Free)
                    {
                        var necessary = band.Quantity - Context.AssetSpotBalance.Free;

                        if (_options.RedeemAssetSavings)
                        {
                            _logger.LogInformation(
                                "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity} {Asset} for {Price} {Quote} but there is only {Free} {Asset} available. Will attempt to redeem {Necessary} {Asset} rest from savings.",
                                TypeName, Context.Name, OrderType.Limit, OrderSide.Sell, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, Context.AssetSpotBalance.Free, Context.Symbol.BaseAsset, necessary, Context.Symbol.BaseAsset);

                            var result = await TryRedeemSavings(Context.Symbol.BaseAsset, necessary)
                                .ExecuteAsync(Context, cancellationToken)
                                .ConfigureAwait(false);

                            if (result.Success)
                            {
                                _logger.LogInformation(
                                    "{Type} {Name} redeemed {Amount} {Asset} successfully",
                                    TypeName, Context.Name, necessary, Context.Symbol.BaseAsset);

                                // let the algo cycle to allow redemption to process
                                return Noop();
                            }
                            else
                            {
                                _logger.LogError(
                                   "{Type} {Name} cannot set band sell order of {Quantity} {Asset} for {Price} {Quote} because there are only {Balance} {Asset} free and savings redemption failed",
                                    TypeName, Context.Name, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, Context.AssetSpotBalance.Free, Context.Symbol.BaseAsset);

                                if (_options.RedeemAssetSwapPool)
                                {
                                    _logger.LogInformation(
                                        "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity} {Asset} for {Price} {Quote} but there is only {Free} {Asset} available. Will attempt to redeem {Necessary} {Asset} rest from a swap pool",
                                        TypeName, Context.Name, OrderType.Limit, OrderSide.Sell, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, Context.AssetSpotBalance.Free, Context.Symbol.BaseAsset, necessary, Context.Symbol.BaseAsset);

                                    var result2 = await TryRedeemSwapPool(Context.Symbol.BaseAsset, necessary)
                                        .ExecuteAsync(Context, cancellationToken)
                                        .ConfigureAwait(false);

                                    if (result2.Success)
                                    {
                                        _logger.LogInformation(
                                            "{Type} {Name} redeemed {Amount} {Asset} successfully",
                                            TypeName, Context.Name, necessary, Context.Symbol.BaseAsset);

                                        // let the algo cycle to allow redemption to process
                                        return Noop();
                                    }
                                    else
                                    {
                                        _logger.LogError(
                                           "{Type} {Name} cannot set band sell order of {Quantity} {Asset} for {Price} {Quote} because there are only {Balance} {Asset} free and swap pool redemption failed",
                                            TypeName, Context.Name, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, Context.AssetSpotBalance.Free, Context.Symbol.BaseAsset);

                                        return null;
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning(
                                        "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity} {Asset} for {Price} {Quote} but there is only {Free} {Asset} available and swap pool redemption is disabled.",
                                        TypeName, Context.Name, OrderType.Limit, OrderSide.Sell, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, Context.AssetSpotBalance.Free);

                                    return null;
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "{Type} {Name} must place {OrderType} {OrderSide} of {Quantity} {Asset} for {Price} {Quote} but there is only {Free} {Asset} available and savings redemption is disabled.",
                                TypeName, Context.Name, OrderType.Limit, OrderSide.Sell, band.Quantity, Context.Symbol.BaseAsset, band.ClosePrice, Context.Symbol.QuoteAsset, Context.AssetSpotBalance.Free);

                            return null;
                        }
                    }

                    var tag = CreateTag(Context.Symbol.Name, band.ClosePrice);
                    return CreateOrder(Context.Symbol, OrderType.Limit, OrderSide.Sell, TimeInForce.GoodTillCanceled, band.Quantity, band.ClosePrice, tag);
                }
            }

            return null;
        }
    }
}