using AutoMapper;
using System.Collections.Immutable;
using System.Linq;
using Outcompute.Trader.Models;

namespace Outcompute.Trader.Trading.Binance.Converters
{
    internal class OrderBookConverter : ITypeConverter<OrderBookModel, OrderBook>
    {
        public OrderBook Convert(OrderBookModel source, OrderBook destination, ResolutionContext context)
        {
            if (source is null) return null!;

            return new OrderBook(
                source.LastUpdateId,
                source.Bids.Select(x => new Bid(x[0], x[1])).ToImmutableList(),
                source.Asks.Select(x => new Ask(x[0], x[1])).ToImmutableList());
        }
    }
}