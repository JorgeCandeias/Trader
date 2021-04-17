using System;
using System.Buffers;
using Trader.Core.Serializers;
using Xunit;

namespace Trader.Core.Tests
{
    public class Base62NumberSerializerTests
    {
        [Fact]
        public void SerializeRefusesNegativeArgument()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Serialize(-1));

            // assert
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public void SerializeHandlesZero()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var result = serializer.Serialize(0);

            // assert
            Assert.Equal("", result);
        }

        [Fact]
        public void SerializeHandlesOne()
        {
            // arrange
            var serializer = new Base62NumberSerializer();
            using var buffer = MemoryPool<char>.Shared.Rent(100);

            // act
            var result = serializer.Serialize(1);

            // assert
            Assert.Equal("1", result);
        }

        [Fact]
        public void SerializeHandlesHighNumber()
        {
            // arrange
            var serializer = new Base62NumberSerializer();
            using var buffer = MemoryPool<char>.Shared.Rent(100);

            // act
            var result = serializer.Serialize(long.MaxValue);

            // assert
            Assert.Equal("aZl8N0y58M7", result);
        }

        [Fact]
        public void SerializeManyRefusesNegativeArgument()
        {
            // arrange
            var serializer = new Base62NumberSerializer();
            var items = new[] { 123L, -1L, 234L };
            using var buffer = MemoryPool<char>.Shared.Rent(100);

            // act
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Serialize(items));

            // assert
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public void SerializeManyHandlesSequence()
        {
            // arrange
            var serializer = new Base62NumberSerializer();
            var items = new[] { 0L, 1L, long.MaxValue, 0L, long.MaxValue, 2L, 0L, 3L };
            using var buffer = MemoryPool<char>.Shared.Rent(100);

            // act
            var result = serializer.Serialize(items);

            // assert
            Assert.Equal("_1_aZl8N0y58M7__aZl8N0y58M7_2__3", result);
        }

        [Fact]
        public void DeserializeOneThrowsOnNullValue()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var exception = Assert.Throws<ArgumentNullException>(() => serializer.DeserializeOne(null));

            // assert
            Assert.Equal("value", exception.ParamName);
        }

        [Fact]
        public void DeserializeOneHandlesEmpty()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var result = serializer.DeserializeOne("");

            // assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void DeserializeOneHandlesZero()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var result = serializer.DeserializeOne("0");

            // assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void DeserializeOneHandlesOne()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var result = serializer.DeserializeOne("1");

            // assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void DeserializeOneHandlesHighNumber()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var result = serializer.DeserializeOne("aZl8N0y58M7");

            // assert
            Assert.Equal(long.MaxValue, result);
        }

        [Fact]
        public void DeserializeManyThrowsOnNullValue()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var exception = Assert.Throws<ArgumentNullException>(() => serializer.DeserializeMany(null));

            // assert
            Assert.Equal("values", exception.ParamName);
        }

        [Fact]
        public void DeserializeManyHandlesEmpty()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var result = serializer.DeserializeMany("");

            // assert
            Assert.Collection(result, x => Assert.Equal(0, x));
        }

        [Fact]
        public void DeserializeManyHandlesZero()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var result = serializer.DeserializeMany("0");

            // assert
            Assert.Collection(result, x => Assert.Equal(0, x));
        }

        [Fact]
        public void DeserializeManyHandlesOne()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var result = serializer.DeserializeMany("1");

            // assert
            Assert.Collection(result, x => Assert.Equal(1, x));
        }

        [Fact]
        public void DeserializeManyHandlesHighNumber()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var result = serializer.DeserializeMany("aZl8N0y58M7");

            // assert
            Assert.Collection(result, x => Assert.Equal(long.MaxValue, x));
        }

        [Fact]
        public void DeserializeManyHandlesSequence()
        {
            // arrange
            var serializer = new Base62NumberSerializer();

            // act
            var result = serializer.DeserializeMany("_1_aZl8N0y58M7__aZl8N0y58M7_2__3");

            // assert
            Assert.Collection(
                result,
                x => Assert.Equal(0, x),
                x => Assert.Equal(1, x),
                x => Assert.Equal(long.MaxValue, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(long.MaxValue, x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(3, x));
        }
    }
}