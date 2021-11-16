using Microsoft.Extensions.DependencyInjection;
using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Providers;

namespace Outcompute.Trader.Trading.Algorithms;

public static class TickerProviderAlgoContextExtensions
{
    public static ITickerProvider GetTickerProvider(this IAlgoContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return context.ServiceProvider.GetRequiredService<ITickerProvider>();
    }

    public static Task<MiniTicker?> TryGetTickerAsync(this IAlgoContext context, string symbol, CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return context.ServiceProvider.GetRequiredService<ITickerProvider>().TryGetTickerAsync(symbol, cancellationToken);
    }

    public static Task<MiniTicker> GetRequiredTickerAsync(this IAlgoContext context, string symbol, CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        return context.ServiceProvider.GetRequiredService<ITickerProvider>().GetRequiredTickerAsync(symbol, cancellationToken);
    }
}