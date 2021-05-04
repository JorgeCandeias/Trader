using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;
using Trader.Models;

namespace Trader.Trading.Binance.Handlers
{
    internal class BinanceApiUsagePostHandler : DelegatingHandler
    {
        private readonly BinanceOptions _options;
        private readonly BinanceUsageContext _usage;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;

        public BinanceApiUsagePostHandler(IOptions<BinanceOptions> options, BinanceUsageContext usage, ILogger<BinanceApiUsagePostHandler> logger, ISystemClock clock)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _usage = usage ?? throw new ArgumentNullException(nameof(usage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Name => nameof(BinanceApiUsagePostHandler);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

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

                        _usage.SetUsed(RateLimitType.RequestWeight, window, weight);
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

                        _usage.SetUsed(RateLimitType.Orders, window, count);
                    }
                }
            }

            // analyse the usages
            foreach (var item in _usage.EnumerateAll())
            {
                var ratio = item.Used / (double)item.Limit;

                if (ratio >= _options.UsageWarningRatio)
                {
                    _logger.LogWarning(
                        "{Name} detected rate limit usage for {RateLimitType} {Window} is at {Usage:P2}",
                        Name, item.Type, item.Window, ratio);

                    // force backoff once the safety limit is reached
                    if (ratio >= _options.UsageBackoffRatio)
                    {
                        _logger.LogWarning(
                            "{Name} detected rate limit usage for {RateLimitType} {Window} is over the limit of {Limit:P2} and will force backoff",
                            Name, item.Type, item.Window, _options.UsageBackoffRatio);

                        TimeSpan retry;
                        var now = _clock.UtcNow;

                        if (item.Window == TimeSpan.FromMinutes(1))
                        {
                            retry = now.AddMinutes(1).AddSeconds(-now.Second).Subtract(now);
                        }
                        else if (item.Window == TimeSpan.FromHours(1))
                        {
                            retry = now.AddHours(1).AddMinutes(-now.Minute).AddSeconds(-now.Second).Subtract(now);
                        }
                        else if (item.Window == TimeSpan.FromDays(1))
                        {
                            retry = now.AddDays(1).AddHours(-now.Hour).AddMinutes(-now.Minute).AddSeconds(-now.Second).Subtract(now);
                        }
                        else
                        {
                            retry = _options.DefaultBackoffPeriod;

                            _logger.LogError(
                                "{Name} detected violation of unknown rate window {RateLimitType} {Window} and will force the default backoff of {Backoff}",
                                Name, item.Type, item.Window, retry);
                        }

                        throw new BinanceTooManyRequestsException(retry);
                    }
                }
            }

            return response;
        }
    }
}