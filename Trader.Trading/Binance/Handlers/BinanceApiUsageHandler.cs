using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Trader.Models;

namespace Trader.Trading.Binance.Handlers
{
    internal class BinanceApiUsageHandler : DelegatingHandler
    {
        private readonly BinanceOptions _options;

        public BinanceApiUsageHandler(IOptions<BinanceOptions> options)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (BinanceApiContext.CaptureUsage)
            {
                foreach (var header in response.Headers)
                {
                    if (header.Key.StartsWith(_options.UsedRequestWeightHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var key = header.Key[_options.UsedRequestWeightHeaderPrefix.Length..];
                        var unit = key[^1..].ToUpperInvariant();
                        var value = int.Parse(key[..^1], CultureInfo.InvariantCulture);

                        var window = unit switch
                        {
                            "S" => TimeSpan.FromSeconds(value),
                            "M" => TimeSpan.FromMinutes(value),
                            "H" => TimeSpan.FromHours(value),
                            "D" => TimeSpan.FromDays(value),
                            _ => throw new InvalidOperationException()
                        };

                        foreach (var item in header.Value)
                        {
                            var weight = int.Parse(item, CultureInfo.InvariantCulture);

                            BinanceApiContext.Usage?.Add(new Usage(RateLimitType.RequestWeight, window, weight));
                        }
                    }
                    else if (header.Key.StartsWith(_options.UsedOrderCountHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var key = header.Key[_options.UsedOrderCountHeaderPrefix.Length..];
                        var unit = key[^1..].ToUpperInvariant();
                        var value = int.Parse(key[..^1], CultureInfo.InvariantCulture);

                        var window = unit switch
                        {
                            "S" => TimeSpan.FromSeconds(value),
                            "M" => TimeSpan.FromMinutes(value),
                            "H" => TimeSpan.FromHours(value),
                            "D" => TimeSpan.FromDays(value),
                            _ => throw new InvalidOperationException()
                        };

                        foreach (var item in header.Value)
                        {
                            var count = int.Parse(item, CultureInfo.InvariantCulture);

                            BinanceApiContext.Usage?.Add(new Usage(RateLimitType.Orders, window, count));
                        }
                    }
                }
            }

            return response;
        }
    }
}