using Outcompute.Trader.Models;
using System;

namespace Orleans.Streams
{
    internal static class OrderStreamProviderExtensions
    {
        public static IAsyncStream<OrderQueryResult> GetOrderStream(this IStreamProvider provider, string symbol)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));
            if (symbol is null) throw new ArgumentNullException(nameof(symbol));

            return provider.GetStream<OrderQueryResult>(Guid.Empty, symbol);
        }
    }
}