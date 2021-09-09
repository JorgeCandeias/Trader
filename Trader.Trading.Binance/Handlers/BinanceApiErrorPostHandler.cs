using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Outcompute.Trader.Core.Time;

namespace Outcompute.Trader.Trading.Binance.Handlers
{
    internal class BinanceApiErrorPostHandler : DelegatingHandler
    {
        private readonly BinanceOptions _options;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;

        public BinanceApiErrorPostHandler(IOptions<BinanceOptions> options, ILogger<BinanceApiErrorPostHandler> logger, ISystemClock clock)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Type => nameof(BinanceApiErrorPostHandler);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            // skip handling for successful results
            if (response.IsSuccessStatusCode) return response;

            // handle ip ban and back-off
            if (response.StatusCode is HttpStatusCode.TooManyRequests || response.StatusCode is (HttpStatusCode)418)
            {
                // discover the appropriate retry after time
                var retryAfter = _options.DefaultBackoffPeriod;
                var okay = false;
                if (response.Headers.RetryAfter is not null)
                {
                    if (response.Headers.RetryAfter.Date.HasValue && response.Headers.RetryAfter.Date.Value > _clock.UtcNow)
                    {
                        _logger.LogWarning(
                            "{Type} received {HttpStatusCode} requesting to wait until {RetryAfterDateTimeOffset}",
                            Type, response.StatusCode, response.Headers.RetryAfter.Date.Value);

                        retryAfter = response.Headers.RetryAfter.Date.Value.Subtract(_clock.UtcNow).Add(TimeSpan.FromSeconds(1));
                        okay = true;
                    }
                    else if (response.Headers.RetryAfter.Delta.HasValue && response.Headers.RetryAfter.Delta.Value > TimeSpan.Zero)
                    {
                        _logger.LogWarning(
                            "{Type} received {HttpStatusCode} requesting to wait for {RetryAfter}",
                            Type, response.StatusCode, response.Headers.RetryAfter.Delta.Value);

                        retryAfter = response.Headers.RetryAfter.Delta.Value.Add(TimeSpan.FromSeconds(1));
                        okay = true;
                    }
                }

                if (!okay)
                {
                    _logger.LogWarning(
                        "{Type} received http status code {HttpStatusCode} without a retry-after header and will use a default of {RetryAfter}",
                        Type, response.StatusCode, retryAfter);
                }

                throw new BinanceTooManyRequestsException(retryAfter);
            }

            // attempt graceful handling of a binance api error
            var error = await response.Content.ReadFromJsonAsync<ErrorModel>(null, cancellationToken).ConfigureAwait(false);
            if (error is not null && error.Code != 0)
            {
                throw new BinanceCodeException(error.Code, error.Msg, response.StatusCode);
            }

            // otherwise default to the standard exception
            response.EnsureSuccessStatusCode();

            return response;
        }
    }
}