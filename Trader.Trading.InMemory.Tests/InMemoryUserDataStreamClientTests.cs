using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.InMemory.UserData;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.InMemory.Tests
{
    public class InMemoryUserDataStreamClientTests
    {
        [Fact]
        public async Task Cycles()
        {
            // arrange
            var message = new UserDataStreamMessage();
            var sender = new InMemoryUserDataStreamSender();
            using var client = new InMemoryUserDataStreamClient(sender);

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