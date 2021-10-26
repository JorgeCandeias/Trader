using System.Collections.Generic;
using Xunit;

namespace Outcompute.Trader.Core.Tests
{
    public class DictionaryExtensionsTests
    {
        [Fact]
        public void GetOrCreateReturnsExistingValue()
        {
            // arrange
            var dictionary = new Dictionary<int, int>
            {
                { 1, 100 }
            };

            // act
            var result = dictionary.GetOrCreate(1, () => 200);

            // assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void GetOrCreateCreatesNewValue()
        {
            // arrange
            var dictionary = new Dictionary<int, int>
            {
                { 1, 100 }
            };

            // act
            var result = dictionary.GetOrCreate(2, () => 200);

            // assert
            Assert.Equal(200, result);
        }

        [Fact]
        public void AddOrUpdateAddsNewValue()
        {
            // arrange
            var dictionary = new Dictionary<int, int>();

            // act
            var result = dictionary.AddOrUpdate(1, () => 100, x => 200);

            // assert
            Assert.Equal(100, result);
            Assert.Collection(dictionary, x =>
            {
                Assert.Equal(1, x.Key);
                Assert.Equal(100, x.Value);
            });
        }

        [Fact]
        public void AddOrUpdateUpdatesExistingValue()
        {
            // arrange
            var dictionary = new Dictionary<int, int>()
            {
                { 1, 100 }
            };

            // act
            var result = dictionary.AddOrUpdate(1, () => 100, x => 200);

            // assert
            Assert.Equal(200, result);
            Assert.Collection(dictionary, x =>
            {
                Assert.Equal(1, x.Key);
                Assert.Equal(200, x.Value);
            });
        }

        [Fact]
        public void AddOrUpdateWithArgAddsNewValue()
        {
            // arrange
            var dictionary = new Dictionary<int, int>();

            // act
            var result = dictionary.AddOrUpdate(1, arg => arg, (x, arg) => x + arg, 123);

            // assert
            Assert.Equal(123, result);
            Assert.Collection(dictionary, x =>
            {
                Assert.Equal(1, x.Key);
                Assert.Equal(123, x.Value);
            });
        }

        [Fact]
        public void AddOrUpdateWithArgUpdatesExistingValue()
        {
            // arrange
            var dictionary = new Dictionary<int, int>()
            {
                { 1, 100 }
            };

            // act
            var result = dictionary.AddOrUpdate(1, arg => arg, (x, arg) => x + arg, 123);

            // assert
            Assert.Equal(223, result);
            Assert.Collection(dictionary, x =>
            {
                Assert.Equal(1, x.Key);
                Assert.Equal(223, x.Value);
            });
        }
    }
}