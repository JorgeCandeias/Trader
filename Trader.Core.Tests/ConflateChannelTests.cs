using Outcompute.Trader.Core.Tasks.Dataflow;
using System.Collections.Immutable;

namespace Outcompute.Trader.Core.Tests
{
    public class ConflateChannelTests
    {
        [Fact]
        public async Task ConflatesSequentialValueType()
        {
            // arrange
            var channel = new ConflateChannel<int, int, int>(() => 0, (c, i) => c + i, c => c);

            // act - send one item
            await channel.Writer.WriteAsync(1);

            // act - read first output
            var result1 = await channel.Reader.ReadAsync(CancellationToken.None);

            // act - send three items
            await channel.Writer.WriteAsync(2);
            await channel.Writer.WriteAsync(4);
            await channel.Writer.WriteAsync(8);

            // act - read second output
            var result2 = await channel.Reader.ReadAsync(CancellationToken.None);

            // assert
            Assert.Equal(1, result1);
            Assert.Equal(14, result2);
        }

        [Fact]
        public async Task ConflatesWaitingValueType()
        {
            // arrange
            var channel = new ConflateChannel<int, int, int>(() => 0, (c, i) => c + i, c => c);

            // act - start waiting for a result
            var result1Task = channel.Reader.ReadAsync();

            // act - send three items and allow the reader to pick them up
            await channel.Writer.WriteAsync(1);
            await Task.Delay(100);

            // act - send more items while the read is not waiting
            await channel.Writer.WriteAsync(2);
            await channel.Writer.WriteAsync(4);

            // act - start waiting for another result
            var result2Task = channel.Reader.ReadAsync();

            // act - complete waiting
            var result1 = await result1Task;
            var result2 = await result2Task;

            // assert
            Assert.Equal(1, result1);
            Assert.Equal(6, result2);
        }

        [Fact]
        public async Task ConflatesSequentialReferenceType()
        {
            // arrange
            var channel = new ConflateChannel<int, ImmutableSortedSet<int>.Builder, ImmutableSortedSet<int>>(
                () => ImmutableSortedSet.CreateBuilder<int>(),
                (builder, value) => { builder.Add(value); return builder; },
                builder => builder.ToImmutable());

            // act - send one item
            await channel.Writer.WriteAsync(1);

            // act - read first output
            var result1 = await channel.Reader.ReadAsync();

            // act - send three distinct items
            await channel.Writer.WriteAsync(1);
            await channel.Writer.WriteAsync(2);
            await channel.Writer.WriteAsync(4);

            // act - read second output
            var result2 = await channel.Reader.ReadAsync();

            // act - send six mixed distinct items
            await channel.Writer.WriteAsync(1);
            await channel.Writer.WriteAsync(2);
            await channel.Writer.WriteAsync(1);
            await channel.Writer.WriteAsync(3);
            await channel.Writer.WriteAsync(2);
            await channel.Writer.WriteAsync(4);

            // act - read third output
            var result3 = await channel.Reader.ReadAsync();

            // assert
            Assert.Collection(result1,
                x => Assert.Equal(1, x));

            Assert.Collection(result2,
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(4, x));

            Assert.Collection(result3,
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(3, x),
                x => Assert.Equal(4, x));
        }
    }
}