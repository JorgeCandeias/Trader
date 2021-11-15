using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Context;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class NoopAlgoCommandTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var command = NoopAlgoCommand.Instance;
            var context = AlgoContext.Empty;

            // act
            await command.ExecuteAsync(context);

            // assert
            Assert.True(true);
        }
    }
}