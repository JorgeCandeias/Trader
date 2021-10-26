using Outcompute.Trader.Models;
using Outcompute.Trader.Trading.InMemory.UserData;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.InMemory.Tests
{
    public class InMemoryUserDataStreamSenderTests
    {
        [Fact]
        public async Task Sends()
        {
            // arrange
            var sender = new InMemoryUserDataStreamSender();
            var message = new UserDataStreamMessage();

            // act
            UserDataStreamMessage? received = null;
            var completion = new TaskCompletionSource();
            using var reg = sender.Register((m, ct) =>
            {
                received = m;
                completion.SetResult();
                return Task.CompletedTask;
            });
            await sender.SendAsync(message);

            // assert
            await Task.WhenAny(completion.Task, Task.Delay(100));
            Assert.Same(message, received);
        }
    }
}