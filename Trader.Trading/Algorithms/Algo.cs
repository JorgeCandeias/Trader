using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.AveragingSell;
using Outcompute.Trader.Trading.Commands.CancelOpenOrders;
using Outcompute.Trader.Trading.Commands.CancelOrder;
using Outcompute.Trader.Trading.Commands.CreateOrder;
using Outcompute.Trader.Trading.Commands.EnsureSingleOrder;
using Outcompute.Trader.Trading.Commands.MarketBuy;
using Outcompute.Trader.Trading.Commands.MarketSell;
using Outcompute.Trader.Trading.Commands.RedeemSavings;
using Outcompute.Trader.Trading.Commands.RedeemSwapPool;
using Outcompute.Trader.Trading.Commands.Sequence;
using Outcompute.Trader.Trading.Commands.SignificantAveragingSell;
using Outcompute.Trader.Trading.Commands.TrackingBuy;

namespace Outcompute.Trader.Trading.Algorithms;

/// <summary>
/// Base class for algos that do not follow the suggested lifecycle.
/// For symbol based algos, consider implementing <see cref="SymbolAlgo"/> instead.
/// </summary>
public abstract class Algo : IAlgo
{
    protected Algo()
    {
        // pin the scoped context created by the factory
        Context = AlgoContext.Current;
    }

    public async ValueTask<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default)
    {
        await Context.UpdateAsync(cancellationToken).ConfigureAwait(false);

        return await OnExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    protected virtual ValueTask<IAlgoCommand> OnExecuteAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(OnExecute());
    }

    protected virtual IAlgoCommand OnExecute()
    {
        return Noop();
    }

    public virtual ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public virtual ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public IAlgoContext Context { get; }

    public virtual IAlgoCommand Noop()
    {
        return NoopAlgoCommand.Instance;
    }

    public virtual IAlgoCommand Sequence(IEnumerable<IAlgoCommand> results)
    {
        return new SequenceCommand(results);
    }

    public virtual IAlgoCommand Sequence(params IAlgoCommand[] results)
    {
        return new SequenceCommand(results);
    }

    #region Command Helpers

    public virtual IAlgoCommand AveragingSell(Symbol symbol, decimal minSellRate, bool redeemSavings = false, bool redeemSwapPool = false)
    {
        return new AveragingSellCommand(symbol, minSellRate, redeemSavings, redeemSwapPool);
    }

    /// <inheritdoc cref="CreateOrderCommand(Symbol, OrderType, OrderSide, TimeInForce?, decimal?, decimal?, decimal?, decimal?, string?)"/>
    public virtual IAlgoCommand CreateOrder(Symbol symbol, OrderType type, OrderSide side, TimeInForce timeInForce, decimal? quantity, decimal? notional, decimal? price, decimal? stopPrice, string? tag)
    {
        return new CreateOrderCommand(symbol, type, side, timeInForce, quantity, notional, price, stopPrice, tag);
    }

    public virtual IAlgoCommand CancelOrder(Symbol symbol, long orderId)
    {
        return new CancelOrderCommand(symbol, orderId);
    }

    public virtual IAlgoCommand EnsureSingleOrder(Symbol symbol, OrderSide side, OrderType type, TimeInForce? timeInForce, decimal? quantity, decimal? notional, decimal? price, decimal? stopPrice, string? tag = null, bool redeemSavings = false, bool redeemSwapPool = false)
    {
        return new EnsureSingleOrderCommand(symbol, side, type, timeInForce, quantity, notional, price, stopPrice, tag, redeemSavings, redeemSwapPool);
    }

    public virtual IAlgoCommand CancelOpenOrders(Symbol symbol, OrderSide? side = null, decimal? distance = null, string? tag = null)
    {
        return new CancelOpenOrdersCommand(symbol, side, distance, tag);
    }

    public virtual IAlgoCommand TryRedeemSavings(string asset, decimal amount)
    {
        return new RedeemSavingsCommand(asset, amount);
    }

    public virtual IAlgoCommand TryRedeemSwapPool(string asset, decimal amount)
    {
        return new RedeemSwapPoolCommand(asset, amount);
    }

    public virtual IAlgoCommand SignificantAveragingSell(Symbol symbol, decimal minimumProfitRate, bool redeemSavings, bool redeemSwapPool)
    {
        return new SignificantAveragingSellCommand(symbol, minimumProfitRate, redeemSavings, redeemSwapPool);
    }

    public virtual IAlgoCommand TrackingBuy(Symbol symbol, decimal pullbackRatio, decimal targetQuoteBalanceFractionPerBuy, decimal? maxNotional, bool redeemSavings, bool redeemSwapPool)
    {
        return new TrackingBuyCommand(symbol, pullbackRatio, targetQuoteBalanceFractionPerBuy, maxNotional, redeemSavings, redeemSwapPool);
    }

    public virtual IAlgoCommand MarketSell(Symbol symbol, decimal quantity, string? tag = null, bool redeemSavings = false, bool redeemSwapPool = false)
    {
        return new MarketSellCommand(symbol, quantity, tag, redeemSavings, redeemSwapPool);
    }

    /// <inheritdoc cref="MarketBuyCommand(Symbol, decimal?, decimal?, bool, bool, bool, bool)" />
    public virtual IAlgoCommand MarketBuy(Symbol symbol, decimal? quantity, decimal? notional, bool raiseToMin, bool raiseToStepSize, bool redeemSavings = false, bool redeemSwapPool = false)
    {
        return new MarketBuyCommand(symbol, quantity, notional, raiseToMin, raiseToStepSize, redeemSavings, redeemSwapPool);
    }

    #endregion Command Helpers
}