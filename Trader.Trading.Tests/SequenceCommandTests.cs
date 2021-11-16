using Microsoft.Extensions.DependencyInjection;
using Moq;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using Outcompute.Trader.Trading.Commands.Sequence;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class SequenceCommandTests
    {
        [Fact]
        public async Task ExecutesParams()
        {
            // arrange
            var sub1 = Mock.Of<IAlgoCommand>();
            var sub2 = Mock.Of<IAlgoCommand>();
            var sub3 = Mock.Of<IAlgoCommand>();

            var command = new SequenceCommand(sub1, sub2, sub3);

            var executor = Mock.Of<IAlgoCommandExecutor<SequenceCommand>>();

            var provider = new ServiceCollection()
                .AddSingleton(executor)
                .BuildServiceProvider();

            var context = new AlgoContext("Algo1", provider);

            // act
            await command.ExecuteAsync(context);

            // assert
            Assert.Collection(command.Commands,
                x => Assert.Same(sub1, x),
                x => Assert.Same(sub2, x),
                x => Assert.Same(sub3, x));

            Mock.Get(executor).Verify(x => x.ExecuteAsync(context, command, CancellationToken.None));
        }

        [Fact]
        public async Task ExecutesEnumerable()
        {
            // arrange
            var sub1 = Mock.Of<IAlgoCommand>();
            var sub2 = Mock.Of<IAlgoCommand>();
            var sub3 = Mock.Of<IAlgoCommand>();
            var subs = new List<IAlgoCommand> { sub1, sub2, sub3 };

            var command = new SequenceCommand(subs);

            var executor = Mock.Of<IAlgoCommandExecutor<SequenceCommand>>();

            var provider = new ServiceCollection()
                .AddSingleton(executor)
                .BuildServiceProvider();

            var context = new AlgoContext("Algo1", provider);

            // act
            await command.ExecuteAsync(context);

            // assert
            Assert.Collection(command.Commands,
                x => Assert.Same(sub1, x),
                x => Assert.Same(sub2, x),
                x => Assert.Same(sub3, x));

            Mock.Get(executor).Verify(x => x.ExecuteAsync(context, command, CancellationToken.None));
        }
    }
}