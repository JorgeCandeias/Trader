using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Outcompute.Trader.Data;

public sealed class FXMacroDataClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public FXMacroDataClient(HttpClient httpClient, string apiKey = "", string baseUrl = "https://api.fxmacrodata.com/v1")
    {
        _httpClient = httpClient;
        _apiKey = apiKey ?? string.Empty;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public Task<string> DataCatalogueAsync(string currency, CancellationToken cancellationToken = default) => GetAsync($"/data_catalogue/{Normalize(currency)}", cancellationToken);
    public Task<string> AnnouncementsAsync(string currency, string indicator, int? limit = null, int? offset = null, CancellationToken cancellationToken = default) => GetAsync(Page($"/announcements/{Normalize(currency)}/{indicator}", limit, offset), cancellationToken);
    public Task<string> CalendarAsync(string currency, CancellationToken cancellationToken = default) => GetAsync($"/calendar/{Normalize(currency)}", cancellationToken);
    public Task<string> PredictionsAsync(string currency, string indicator, int? limit = null, int? offset = null, CancellationToken cancellationToken = default) => GetAsync(Page($"/predictions/{Normalize(currency)}/{indicator}", limit, offset), cancellationToken);
    public Task<string> ForexAsync(string baseCurrency, string quoteCurrency, int? limit = null, int? offset = null, CancellationToken cancellationToken = default) => GetAsync(Page($"/forex/{Normalize(baseCurrency)}/{Normalize(quoteCurrency)}", limit, offset), cancellationToken);
    public Task<string> CotAsync(string currency, int? limit = null, int? offset = null, CancellationToken cancellationToken = default) => GetAsync(Page($"/cot/{Normalize(currency)}", limit, offset), cancellationToken);
    public Task<string> CommoditiesLatestAsync(CancellationToken cancellationToken = default) => GetAsync("/commodities/latest", cancellationToken);
    public Task<string> CommodityAsync(string indicator, int? limit = null, int? offset = null, CancellationToken cancellationToken = default) => GetAsync(Page($"/commodities/{indicator}", limit, offset), cancellationToken);
    public Task<string> CurvesAsync(string currency, CancellationToken cancellationToken = default) => GetAsync($"/curves/{Normalize(currency)}", cancellationToken);
    public Task<string> CurveProxiesAsync(string currency, CancellationToken cancellationToken = default) => GetAsync($"/curve_proxies/{Normalize(currency)}", cancellationToken);
    public Task<string> ForwardCurvesAsync(string currency, CancellationToken cancellationToken = default) => GetAsync($"/forward_curves/{Normalize(currency)}", cancellationToken);
    public Task<string> MarketSessionsAsync(CancellationToken cancellationToken = default) => GetAsync("/market_sessions", cancellationToken);
    public Task<string> RiskSentimentAsync(int? limit = null, int? offset = null, CancellationToken cancellationToken = default) => GetAsync(Page("/risk_sentiment", limit, offset), cancellationToken);
    public Task<string> NewsAsync(string currency, CancellationToken cancellationToken = default) => GetAsync($"/news/{Normalize(currency)}", cancellationToken);
    public Task<string> PressReleasesAsync(string currency, CancellationToken cancellationToken = default) => GetAsync($"/press-releases/{Normalize(currency)}", cancellationToken);

    private async Task<string> GetAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _baseUrl + path);
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.Add("X-API-Key", _apiKey);
        }
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    // History endpoints return 20 rows by default and at most 100 per request, newest first.
    // Step offset forward while the response "pagination" object reports "has_more": true.
    private static string Page(string path, int? limit, int? offset)
    {
        var query = new List<string>();
        if (limit.HasValue) query.Add($"limit={limit.Value}");
        if (offset.HasValue) query.Add($"offset={offset.Value}");
        return query.Count == 0 ? path : path + "?" + string.Join("&", query);
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
