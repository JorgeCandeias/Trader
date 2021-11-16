using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms;

public static class ExchangeInfoProviderAlgoContextExtensions
{
    public static IExchangeInfoProvider GetExchangeInfoProvider(this IAlgoContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return context.ServiceProvider.GetRequiredService<IExchangeInfoProvider>();
    }

    public static Task<Symbol> GetRequiredSymbolAsync(this IAlgoContext context, string symbol, CancellationToken cancellationToken = default)
    {
        return context.GetExchangeInfoProvider().GetRequiredSymbolAsync(symbol, cancellationToken);
    }
}