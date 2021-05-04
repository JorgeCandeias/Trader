using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Trader.Core.Time;

namespace Trader.Trading.Binance
{
    internal class BinanceApiHandler : DelegatingHandler
    {
        private readonly BinanceOptions _options;
        private readonly ILogger _logger;
        private readonly ISystemClock _clock;

        public BinanceApiHandler(IOptions<BinanceOptions> options, ILogger<BinanceApiHandler> logger, ISystemClock clock)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private static string Type => nameof(BinanceApiHandler);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // skip handling for successful results
            if (response.IsSuccessStatusCode) return response;

            // handle ip ban and back-off
            if (response.StatusCode is HttpStatusCode.TooManyRequests || response.StatusCode is (HttpStatusCode)418)
            {
                // discover the appropriate retry after time
                DateTime? retryAfterUtc = null;
                if (response.Headers.RetryAfter is not null)
                {
                    if (response.Headers.RetryAfter.Date.HasValue)
                    {
                        _logger.LogWarning(
                            "{Type} received {HttpStatusCode} requesting to wait until {RetryAfterDateTimeOffset}",
                            Type, response.StatusCode, response.Headers.RetryAfter.Date.Value);

                        retryAfterUtc = response.Headers.RetryAfter.Date.Value.UtcDateTime.AddSeconds(1);
                    }
                    else if (response.Headers.RetryAfter.Delta.HasValue)
                    {
                        _logger.LogWarning(
                            "{Type} received {HttpStatusCode} requesting to wait for {RetryAfterTimeSpan}",
                            Type, response.StatusCode, response.Headers.RetryAfter.Delta.Value);

                        retryAfterUtc = _clock.UtcNow.Add(response.Headers.RetryAfter.Delta.Value).AddSeconds(1);
                    }
                }

                if (!retryAfterUtc.HasValue)
                {
                    _logger.LogWarning(
                        "{Type} received http status code {HttpStatusCode} without a retry-after header and will use a default of {RetyrAfterTimeSpan}",
                        Type, response.StatusCode, _options.DefaultBackoffPeriod);

                    retryAfterUtc = _clock.UtcNow.Add(_options.DefaultBackoffPeriod).AddSeconds(1);
                }

                throw new BinanceTooManyRequestsException(retryAfterUtc.Value);
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