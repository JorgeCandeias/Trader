﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Outcompute.Trader.Hosting
{
    public interface ITraderHostBuilder
    {
        public ITraderHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure);

        public ITraderHostBuilder ConfigureTrader(Action<HostBuilderContext, ITraderHostBuilder> configure);
    }
}