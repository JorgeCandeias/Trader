using Microsoft.Extensions.DependencyInjection;
using System;
using Outcompute.Trader.Trading;

namespace Outcompute.Trader.Hosting
{
    public static class TraderAgentTraderHostBuilderExtensions
    {
        public static ITraderHostBuilder ConfigureTraderAgent(this ITraderHostBuilder trader, Action<TraderAgentOptions> configure)
        {
            if (trader is null) throw new ArgumentNullException(nameof(trader));
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            return trader.ConfigureServices(services =>
            {
                services.Configure(configure);
            });
        }
    }
}