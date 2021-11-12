using Microsoft.Extensions.DependencyInjection;
using System;

namespace Outcompute.Trader.Trading.Algorithms
{
    internal class AlgoBuilder<TAlgo> : IAlgoBuilder<TAlgo>
        where TAlgo : IAlgo
    {
        public AlgoBuilder(string name, IServiceCollection services)
        {
            Name = name;
            Services = services;
        }

        public string Name { get; }

        public IServiceCollection Services { get; }

        protected void ConfigureTypeOptionsCore<TOptions>(Action<TOptions> configure)
            where TOptions : class
        {
            Services
                .AddOptions<TOptions>(Name)
                .Configure(configure)
                .ValidateDataAnnotations();
        }

        public IAlgoBuilder<TAlgo> ConfigureHostOptions(Action<AlgoOptions> configure)
        {
            Services.Configure(Name, configure);

            return this;
        }

        public IAlgoBuilder<TAlgo> ConfigureTypeOptions<TOptions>(Action<TOptions> configure)
            where TOptions : class
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            ConfigureTypeOptionsCore(configure);

            return new AlgoBuilder<TAlgo, TOptions>(Name, Services);
        }
    }

    internal class AlgoBuilder<TAlgo, TOptions> : AlgoBuilder<TAlgo>, IAlgoBuilder<TAlgo, TOptions>
        where TAlgo : IAlgo
        where TOptions : class
    {
        public AlgoBuilder(string name, IServiceCollection services) :
            base(name, services)
        {
        }

        public new IAlgoBuilder<TAlgo, TOptions> ConfigureHostOptions(Action<AlgoOptions> configure)
        {
            Services.Configure(Name, configure);

            return this;
        }

        public IAlgoBuilder<TAlgo, TOptions> ConfigureTypeOptions(Action<TOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            ConfigureTypeOptionsCore(configure);

            return new AlgoBuilder<TAlgo, TOptions>(Name, Services);
        }
    }
}