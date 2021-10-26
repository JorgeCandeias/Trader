using Moq;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.Many;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class ManyExecutorTests
    {
        [Fact]
        public async Task Executes()
        {
            // arrange
            var executor = new ManyExecutor();
            var sub1 = Mock.Of<IAlgoCommand>();
            var sub2 = Mock.Of<IAlgoCommand>();
            var sub3 = Mock.Of<IAlgoCommand>();
            var command = new ManyCommand(sub1, sub2, sub3);
            var context = AlgoContext.Empty;

            // act
            await executor.ExecuteAsync(context, command);

            // assert
            Mock.Get(sub1).Verify(x => x.ExecuteAsync(context, CancellationToken.None));
            Mock.Get(sub2).Verify(x => x.ExecuteAsync(context, CancellationToken.None));
            Mock.Get(sub3).Verify(x => x.ExecuteAsync(context, CancellationToken.None));
        }
    }
}