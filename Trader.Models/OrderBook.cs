using System.Collections.Immutable;

namespace Trader.Models
{
    public record OrderBook(
        int LastUpdateId,
        ImmutableList<Bid> Bids,
        ImmutableList<Ask> Asks);

    public record Bid(
        decimal Price,
        decimal Quantity);

    public record Ask(
        decimal Price,
        decimal Quantity);
}