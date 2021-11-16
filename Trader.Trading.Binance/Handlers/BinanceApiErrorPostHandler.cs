using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Outcompute.Trader.Core.Time;
using System.Net;
using System.Net.Http.Json;

namespace Outcompute.Trader.Trading.Binance.Handlers;

internal partial class BinanceApiErrorPostHandler : DelegatingHandler
{
    private readonly BinanceOptions _options;
    private readonly ILogger _logger;
    private readonly ISystemClock _clock;

    public BinanceApiErrorPostHandler(IOptions<BinanceOptions> options, ILogger<BinanceApiErrorPostHandler> logger, ISystemClock clock)
    {
        _options = options.Value;
        _logger = logger;
        _clock = clock;
    }

    private const string Type = nameof(BinanceApiErrorPostHandler);

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
                    LogReceivedRequestToWaitAfterOffset(Type, response.StatusCode, response.Headers.RetryAfter.Date.Value);

                    retryAfter = response.Headers.RetryAfter.Date.Value.Subtract(_clock.UtcNow).Add(TimeSpan.FromSeconds(1));
                    okay = true;
                }
                else if (response.Headers.RetryAfter.Delta.HasValue && response.Headers.RetryAfter.Delta.Value > TimeSpan.Zero)
                {
                    LogReceivedRequestToWaitAfterTimeSpan(Type, response.StatusCode, response.Headers.RetryAfter.Delta.Value);

                    retryAfter = response.Headers.RetryAfter.Delta.Value.Add(TimeSpan.FromSeconds(1));
                    okay = true;
                }
            }

            if (!okay)
            {
                LogReceivedRequestToWaitWithoutRetryAfter(Type, response.StatusCode, retryAfter);
            }

            throw new BinanceTooManyRequestsException(retryAfter);
        }

        // attempt graceful handling of a binance api error
        var error = await response.Content.ReadFromJsonAsync<ApiError>(_options.JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        if (error is not null && error.Code != 0)
        {
            throw new BinanceCodeException(error.Code, error.Msg, response.StatusCode);
        }

        // otherwise default to the standard exception
        response.EnsureSuccessStatusCode();

        return response;
    }

    #region Logging

    [LoggerMessage(0, LogLevel.Warning, "{Type} received {HttpStatusCode} requesting to wait until {RetryAfterDateTimeOffset}")]
    private partial void LogReceivedRequestToWaitAfterOffset(string type, HttpStatusCode httpStatusCode, DateTimeOffset retryAfterDateTimeOffset);

    [LoggerMessage(1, LogLevel.Warning, "{Type} received {HttpStatusCode} requesting to wait for {RetryAfter}")]
    private partial void LogReceivedRequestToWaitAfterTimeSpan(string type, HttpStatusCode httpStatusCode, TimeSpan retryAfter);

    [LoggerMessage(2, LogLevel.Warning, "{Type} received http status code {HttpStatusCode} without a retry-after header and will use a default of {RetryAfter}")]
    private partial void LogReceivedRequestToWaitWithoutRetryAfter(string type, HttpStatusCode httpStatusCode, TimeSpan retryAfter);

    #endregion Logging
}