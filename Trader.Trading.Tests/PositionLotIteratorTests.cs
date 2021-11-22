using Outcompute.Trader.Trading.Algorithms.Positions;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class PositionLotIteratorTests
    {
        [Fact]
        public void ThrowsOnNullSource()
        {
            // arrange
            Position[] source = null!;

            // act
            void Action() => source.EnumerateLots(0);

            // assert
            Assert.Throws<ArgumentNullException>(nameof(source), Action);
        }

        [Fact]
        public void ThrowsOnZeroSize()
        {
            // arrange
            var source = Array.Empty<Position>();
            var size = 0M;

            // act
            void Action() => source.EnumerateLots(size);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(nameof(size), Action);
        }

        [Fact]
        public void ThrowsOnNegativeSize()
        {
            // arrange
            var source = Array.Empty<Position>();
            var size = -1M;

            // act
            void Action() => source.EnumerateLots(size);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(nameof(size), Action);
        }

        [Fact]
        public void EnumeratesEmpty()
        {
            // arrange
            var source = Array.Empty<Position>();
            var size = 10M;

            // act
            var result = source.EnumerateLots(size);

            // assert
            Assert.Empty(result);
        }

        [Fact]
        public void IgnoresLeftovers()
        {
            // arrange
            var source = new[]
            {
                Position.Empty with { Quantity = 9M, Price = 100M }
            };
            var size = 10M;

            // act
            var result = source.EnumerateLots(size);

            // assert
            Assert.Empty(result);
        }

        [Fact]
        public void EnumeratesSingleBlock()
        {
            // arrange
            var source = new[]
            {
                Position.Empty with { Quantity = 10M, Price = 100M }
            };
            var size = 10M;

            // act
            var result = source.EnumerateLots(size);

            // assert
            Assert.Collection(result,
                x =>
                {
                    Assert.Equal(10M, x.Quantity);
                    Assert.Equal(100M, x.AvgPrice);
                });
        }

        [Fact]
        public void EnumeratesTwoEqualBlocks()
        {
            // arrange
            var source = new[]
            {
                Position.Empty with { Quantity = 10M, Price = 123M },
                Position.Empty with { Quantity = 10M, Price = 234M }
            };
            var size = 10M;

            // act
            var result = source.EnumerateLots(size);

            // assert
            Assert.Collection(result,
                x =>
                {
                    Assert.Equal(10M, x.Quantity);
                    Assert.Equal(123M, x.AvgPrice);
                },
                x =>
                {
                    Assert.Equal(10M, x.Quantity);
                    Assert.Equal(234M, x.AvgPrice);
                });
        }

        [Fact]
        public void JoinsTwoHalfBlocks()
        {
            // arrange
            var source = new[]
            {
                Position.Empty with { Quantity = 5M, Price = 123M },
                Position.Empty with { Quantity = 5M, Price = 234M }
            };
            var size = 10M;
            var avg = source.Sum(x => x.Price * x.Quantity) / source.Sum(x => x.Quantity);

            // act
            var result = source.EnumerateLots(size);

            // assert
            Assert.Collection(result,
                x =>
                {
                    Assert.Equal(10M, x.Quantity);
                    Assert.Equal(avg, x.AvgPrice);
                });
        }

        [Fact]
        public void JoinsTwoUnevenBlocks()
        {
            // arrange
            var source = new[]
            {
                Position.Empty with { Quantity = 3M, Price = 123M },
                Position.Empty with { Quantity = 7M, Price = 234M }
            };
            var size = 10M;
            var avg = source.Sum(x => x.Price * x.Quantity) / source.Sum(x => x.Quantity);

            // act
            var result = source.EnumerateLots(size);

            // assert
            Assert.Collection(result,
                x =>
                {
                    Assert.Equal(10M, x.Quantity);
                    Assert.Equal(avg, x.AvgPrice);
                });
        }

        [Fact]
        public void JoinsTwoUnevenBlocksIgnoresLeftovers()
        {
            // arrange
            var source = new[]
            {
                Position.Empty with { Quantity = 3M, Price = 123M },
                Position.Empty with { Quantity = 7M, Price = 234M },
                Position.Empty with { Quantity = 9M, Price = 345M }
            };
            var size = 10M;
            var avg = source.Take(2).Sum(x => x.Price * x.Quantity) / source.Take(2).Sum(x => x.Quantity);

            // act
            var result = source.EnumerateLots(size);

            // assert
            Assert.Collection(result,
                x =>
                {
                    Assert.Equal(10M, x.Quantity);
                    Assert.Equal(avg, x.AvgPrice);
                });
        }

        [Fact]
        public void JoinsFourHalfUnevenBlocksIgnoresLeftovers()
        {
            // arrange
            var source = new[]
            {
                Position.Empty with { Quantity = 3M, Price = 123M }, // +3 =  3
                Position.Empty with { Quantity = 7M, Price = 234M }, // +7 = 10
                Position.Empty with { Quantity = 9M, Price = 345M }, // +9 = 19
                Position.Empty with { Quantity = 3M, Price = 456M }  // +3 = 22 (2 leftovers)
            };
            var size = 10M;

            var notional1 = source.Take(2).Sum(x => x.Price * x.Quantity);
            var quantity1 = source.Take(2).Sum(x => x.Quantity);
            var avg1 = notional1 / quantity1;

            var notional2 = source.TakeLast(2).Sum(x => x.Price * x.Quantity) - source.TakeLast(1).Sum(x => x.Price * 2M);
            var quantity2 = source.TakeLast(2).Sum(x => x.Quantity) - 2M;
            var avg2 = notional2 / quantity2;

            // act
            var result = source.EnumerateLots(size);

            // assert
            Assert.Collection(result,
                x =>
                {
                    Assert.Equal(quantity1, x.Quantity);
                    Assert.Equal(avg1, x.AvgPrice);
                },
                x =>
                {
                    Assert.Equal(quantity2, x.Quantity);
                    Assert.Equal(avg2, x.AvgPrice);
                });
        }

        [Fact]
        public void JoinsFourAllUnevenBlocksIgnoresLeftovers()
        {
            // arrange
            var source = new[]
            {
                Position.Empty with { Quantity = 3M, Price = 123M }, // +3 =  3 (3 for block 1)
                Position.Empty with { Quantity = 8M, Price = 234M }, // +8 = 11 (7 for block 1, 1 for block 2)
                Position.Empty with { Quantity = 7M, Price = 345M }, // +7 = 18 (7 for block 2)
                Position.Empty with { Quantity = 4M, Price = 456M }  // +4 = 22 (2 for block 2, 2 leftovers)
            };
            var size = 10M;

            // expected result 1
            var quantity1 = 3M + 7M;
            var notional1 = (3M * 123M) + (7M * 234M);
            var avg1 = notional1 / quantity1;

            // expected result2
            var quantity2 = 1M + 7M + 2M;
            var notional2 = (1M * 234M) + (7M * 345M) + (2M * 456M);
            var avg2 = notional2 / quantity2;

            // act
            var result = source.EnumerateLots(size);

            // assert
            Assert.Collection(result,
                x =>
                {
                    Assert.Equal(quantity1, x.Quantity);
                    Assert.Equal(avg1, x.AvgPrice);
                },
                x =>
                {
                    Assert.Equal(quantity2, x.Quantity);
                    Assert.Equal(avg2, x.AvgPrice);
                });
        }

        [Fact]
        public void ConsistentLotAverage()
        {
            // arrange
            var source = new[]
            {
                Position.Empty with { Quantity = 3M, Price = 123M }, // +3 =  3 (3 for block 1)
                Position.Empty with { Quantity = 8M, Price = 234M }, // +8 = 11 (7 for block 1, 1 for block 2)
                Position.Empty with { Quantity = 7M, Price = 345M }, // +7 = 18 (7 for block 2)
                Position.Empty with { Quantity = 4M, Price = 456M }  // +4 = 22 (2 for block 2, 2 leftovers)
            };
            var size = 10M;

            // expected result - average of the individual lots
            var quantity = 3M + 7M + 1M + 7M + 2M;
            var notional = (3M * 123M) + (7M * 234M) + (1M * 234M) + (7M * 345M) + (2M * 456M);
            var avg = notional / quantity;

            // act
            var result = source.EnumerateLots(size).Sum(x => x.AvgPrice * x.Quantity) / source.EnumerateLots(size).Sum(x => x.Quantity);

            // assert
            Assert.Equal(avg, result);
        }

        [Fact]
        public void ConsistentSourceAverage()
        {
            // arrange
            var source = new[]
            {
                Position.Empty with { Quantity = 3M, Price = 123M }, // +3 =  3 (3 for block 1)
                Position.Empty with { Quantity = 8M, Price = 234M }, // +8 = 11 (7 for block 1, 1 for block 2)
                Position.Empty with { Quantity = 7M, Price = 345M }, // +7 = 18 (7 for block 2)
                Position.Empty with { Quantity = 4M, Price = 456M }  // +4 = 22 (2 for block 2, 2 leftovers)
            };
            var size = 10M;

            // expected result - average of the elected source
            var quantity = 3M + 8M + 7M + 2M;
            var notional = (3M * 123M) + (8M * 234M) + (7M * 345M) + (2M * 456M);
            var avg = notional / quantity;

            // act
            var result = source.EnumerateLots(size).Sum(x => x.AvgPrice * x.Quantity) / source.EnumerateLots(size).Sum(x => x.Quantity);

            // assert
            Assert.Equal(avg, result);
        }
    }
}