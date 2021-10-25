using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.InMemory.MarketData;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.InMemory.Tests
{
    public class InMemoryMarketDataStreamClientTests
    {
        [Fact]
        public async Task Cycles()
        {
            // arrange
            var message = new MarketDataStreamMessage(null, null, null);
            var sender = new InMemoryMarketDataStreamSender();
            using var client = new InMemoryMarketDataStreamClient(sender);

            // act
            await client.ConnectAsync();
            await sender.SendAsync(message);
            var result = await client.ReceiveAsync();

            // assert
            Assert.Same(message, result);

            // act
            await client.CloseAsync();
        }
    }
}