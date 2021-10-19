﻿using AutoMapper;
using Microsoft.Extensions.Hosting;
using Orleans;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers.Orders
{
    /// <summary>
    /// This hosted service makes services available to <see cref="OrderProviderExtensions"/>.
    /// </summary>
    internal class OrderProviderExtensionsHostedService : IHostedService
    {
        public OrderProviderExtensionsHostedService(IMapper mapper, IGrainFactory factory)
        {
            OrderProviderExtensions.Mapper = mapper;
            OrderProviderExtensions.GrainFactory = factory;
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}