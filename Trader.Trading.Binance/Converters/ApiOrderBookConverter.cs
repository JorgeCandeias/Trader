using AutoMapper;
using Outcompute.Trader.Models;
using System.Collections.Immutable;
using System.Linq;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class ApiOrderBookConverter : ITypeConverter<ApiOrderBook, OrderBook>
    {
        public OrderBook Convert(ApiOrderBook source, OrderBook destination, ResolutionContext context)
        {
            if (source is null) return null!;

            return new OrderBook(
                source.LastUpdateId,
                source.Bids.Select(x => new Bid(x[0], x[1])).ToImmutableList(),
                source.Asks.Select(x => new Ask(x[0], x[1])).ToImmutableList());
        }
    }
}