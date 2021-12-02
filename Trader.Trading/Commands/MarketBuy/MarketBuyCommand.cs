using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;

namespace Outcompute.Trader.Trading.Commands.MarketBuy;

public class MarketBuyCommand : IAlgoCommand
{
    /// <summary>
    /// Creates a market buy order with the specified parameters.
    /// </summary>
    /// <param name="symbol">The symbol for the order.</param>
    /// <param name="quantity">The base asset quantity to buy.</param>
    /// <param name="notional">The quote asset quantity to buy.</param>
    /// <param name="raiseToMin">Whether to raise the <paramref name="quantity"/> or <paramref name="notional"/> to the exchange minimum to ensure a valid order.</param>
    /// <param name="raiseToStepSize">Whether to raise the fractional <paramref name="quantity"/> or <paramref name="notional"/> values to the next step size of the exchange to ensure a valid order.</param>
    /// <param name="tag">Unique tag of the open order.</param>
    internal MarketBuyCommand(Symbol symbol, decimal? quantity, decimal? notional, bool raiseToMin = false, bool raiseToStepSize = false, string? tag = null)
    {
        Guard.IsNotNull(symbol, nameof(symbol));

        if (quantity is null && notional is null)
        {
            ThrowHelper.ThrowArgumentException($"Specify one of '{nameof(quantity)}' or '{nameof(notional)}' arguments");
        }

        if (quantity is not null && notional is not null)
        {
            ThrowHelper.ThrowArgumentException($"Specify only one of '{nameof(quantity)}' or '{nameof(notional)}' and not both");
        }

        Symbol = symbol;
        Quantity = quantity;
        Notional = notional;
        RaiseToMin = raiseToMin;
        RaiseToStepSize = raiseToStepSize;
        Tag = tag;
    }

    public Symbol Symbol { get; }
    public decimal? Quantity { get; }
    public decimal? Notional { get; }
    public bool RaiseToMin { get; }
    public bool RaiseToStepSize { get; }
    public string? Tag { get; }

    public ValueTask ExecuteAsync(IAlgoContext context, CancellationToken cancellationToken = default)
    {
        return context.ServiceProvider
            .GetRequiredService<IAlgoCommandExecutor<MarketBuyCommand>>()
            .ExecuteAsync(context, this, cancellationToken);
    }
}