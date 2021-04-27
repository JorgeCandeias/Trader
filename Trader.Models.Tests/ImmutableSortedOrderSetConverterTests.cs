using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using Trader.Models.Collections;
using Xunit;
using static System.String;

namespace Trader.Models.Tests
{
    public class ImmutableSortedOrderSetConverterTests
    {
        [Fact]
        public void ThrowsOnNullSource()
        {
            // arrange
            var converter = new ImmutableSortedOrderSetConverter<int>();

            // act
            var exception = Assert.Throws<ArgumentNullException>(() => converter.Convert(null!, null!, null!));

            // assert
            Assert.Equal("source", exception.ParamName);
        }

        [Fact]
        public void ThrowsOnNonNullDestination()
        {
            // arrange
            var converter = new ImmutableSortedOrderSetConverter<int>();
            var source = Enumerable.Empty<int>();
            var destination = ImmutableSortedOrderSet.Empty;

            // act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => converter.Convert(source, destination, null!));

            // assert
            Assert.Equal("destination", exception.ParamName);
        }

        [Fact]
        public void ThrowsOnNullContext()
        {
            // arrange
            var converter = new ImmutableSortedOrderSetConverter<int>();
            var source = Enumerable.Empty<int>();

            // act
            var exception = Assert.Throws<ArgumentNullException>(() => converter.Convert(source, null!, null!));

            // assert
            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void MapsSourceToResult()
        {
            // arrange
            var config = new MapperConfiguration(options =>
            {
                options.CreateMap(typeof(IEnumerable<>), typeof(ImmutableSortedOrderSet)).ConvertUsing(typeof(ImmutableSortedOrderSetConverter<>));
                options.CreateMap<int, OrderQueryResult>().ConvertUsing(x => new OrderQueryResult("ZZZ", x, 0, Empty, 0, 0, 0, 0, OrderStatus.None, TimeInForce.None, OrderType.None, OrderSide.None, 0, 0, DateTime.UtcNow, DateTime.UtcNow, false, 0));
            });

            var source = Enumerable.Range(1, 3);

            // act
            var result = config.CreateMapper().Map<ImmutableSortedOrderSet>(source);

            // assert
            Assert.NotNull(result);
            Assert.Collection(result,
                x => Assert.Equal(1, x.OrderId),
                x => Assert.Equal(2, x.OrderId),
                x => Assert.Equal(3, x.OrderId));
        }

        [Fact]
        public void ThrowsOnMappingToDestination()
        {
            // arrange
            var config = new MapperConfiguration(options =>
            {
                options.CreateMap(typeof(IEnumerable<>), typeof(ImmutableSortedOrderSet)).ConvertUsing(typeof(ImmutableSortedOrderSetConverter<>));
                options.CreateMap<int, OrderQueryResult>().ConvertUsing(x => new OrderQueryResult("ZZZ", x, 0, Empty, 0, 0, 0, 0, OrderStatus.None, TimeInForce.None, OrderType.None, OrderSide.None, 0, 0, DateTime.UtcNow, DateTime.UtcNow, false, 0));
            });

            var source = Enumerable.Range(1, 3);
            var destination = ImmutableSortedOrderSet.Empty;

            // act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => config.CreateMapper().Map(source, destination));

            // assert
            Assert.Equal("destination", exception.ParamName);
        }
    }
}