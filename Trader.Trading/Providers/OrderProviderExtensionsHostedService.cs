using AutoMapper;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Trading.Providers
{
    /// <summary>
    /// This hosted service makes services available to <see cref="IOrderProviderExtensions"/>.
    /// </summary>
    internal class OrderProviderExtensionsHostedService : IHostedService
    {
        public OrderProviderExtensionsHostedService(IMapper mapper)
        {
            IOrderProviderExtensions.Mapper = mapper;
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}