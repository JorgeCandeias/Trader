using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Context;
using Outcompute.Trader.Trading.Commands;
using Xunit;

namespace Outcompute.Trader.Trading.Tests
{
    public class AlgoBuilderTests
    {
        private class TestAlgo : IAlgo
        {
            public IAlgoContext Context => throw new NotImplementedException();

            public ValueTask<IAlgoCommand> GoAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public ValueTask StartAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

            public ValueTask StopAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private class TestAlgoOptions
        {
        }

        [Fact]
        public void T1GetsProperties()
        {
            // arrange
            var name = "Algo1";
            var services = new ServiceCollection();

            // act
            var builder = new AlgoBuilder<TestAlgo>(name, services);

            // assert
            Assert.Equal(name, builder.Name);
            Assert.Equal(services, builder.Services);
        }

        [Fact]
        public void T1ConfiguresHostOptions()
        {
            // arrange
            var name = "Algo1";
            var services = Mock.Of<IServiceCollection>();
            var builder = new AlgoBuilder<TestAlgo>(name, services);

            // act
            var result = builder.ConfigureHostOptions(options =>
            {
            });

            // assert
            Assert.Same(builder, result);
            Mock.Get(services).Verify(x => x.Add(It.Is<ServiceDescriptor>(x => x.ServiceType == typeof(IConfigureOptions<AlgoOptions>))));
        }

        [Fact]
        public void T1ConfiguresTypeOptions()
        {
            // arrange
            var name = "Algo1";
            var services = Mock.Of<IServiceCollection>();
            var builder = new AlgoBuilder<TestAlgo>(name, services);

            // act
            var result = builder.ConfigureTypeOptions<TestAlgoOptions>(options =>
            {
            });

            // assert
            Assert.IsType<AlgoBuilder<TestAlgo, TestAlgoOptions>>(result);
            Assert.NotSame(builder, result);
            Assert.Same(builder.Name, result.Name);
            Assert.Same(builder.Services, result.Services);
            Mock.Get(services).Verify(x => x.Add(It.Is<ServiceDescriptor>(x => x.ServiceType == typeof(IConfigureOptions<TestAlgoOptions>))));
        }

        [Fact]
        public void T2GetsProperties()
        {
            // arrange
            var name = "Algo1";
            var services = new ServiceCollection();

            // act
            var builder = new AlgoBuilder<TestAlgo, TestAlgoOptions>(name, services);

            // assert
            Assert.Equal(name, builder.Name);
            Assert.Equal(services, builder.Services);
        }

        [Fact]
        public void T2ConfiguresHostOptions()
        {
            // arrange
            var name = "Algo1";
            var services = Mock.Of<IServiceCollection>();
            var builder = new AlgoBuilder<TestAlgo, TestAlgoOptions>(name, services);

            // act
            var result = builder.ConfigureHostOptions(options =>
            {
            });

            // assert
            Assert.Same(builder, result);
            Mock.Get(services).Verify(x => x.Add(It.Is<ServiceDescriptor>(x => x.ServiceType == typeof(IConfigureOptions<AlgoOptions>))));
        }

        [Fact]
        public void T2ConfiguresTypeOptions()
        {
            // arrange
            var name = "Algo1";
            var services = Mock.Of<IServiceCollection>();
            var builder = new AlgoBuilder<TestAlgo, TestAlgoOptions>(name, services);

            // act
            var result = builder.ConfigureTypeOptions(options =>
            {
            });

            // assert
            Assert.Same(builder, result);
            Assert.Same(builder.Name, result.Name);
            Assert.Same(builder.Services, result.Services);
            Mock.Get(services).Verify(x => x.Add(It.Is<ServiceDescriptor>(x => x.ServiceType == typeof(IConfigureOptions<TestAlgoOptions>))));
        }
    }
}