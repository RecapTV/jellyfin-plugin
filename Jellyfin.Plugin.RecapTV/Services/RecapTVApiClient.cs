using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.RecapTV.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RecapTV.Services
{
    public enum WatchEventResult
    {
        Synced,
        Disabled,
        Unauthorized,
        InvalidEvent,
        Error
    }

    /// <summary>
    /// Talks to POST /jellyfin/webhook on RecapTV. Token validity is established lazily:
    /// there is no separate "verify" endpoint, so a 401 here is what tells us the token is bad.
    /// </summary>
    public class RecapTVApiClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly ILogger<RecapTVApiClient> _logger;

        public RecapTVApiClient(HttpClient httpClient, ILogger<RecapTVApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public Task<WatchEventResult> SendMovieWatchedAsync(string token, int? tmdbId, int? tvdbId, string title, string? year, CancellationToken cancellationToken)
        {
            return SendAsync(token, new Dictionary<string, object?>
            {
                ["type"] = "movie",
                ["tmdbId"] = tmdbId,
                ["tvdbId"] = tvdbId,
                ["title"] = title,
                ["year"] = year
            }, cancellationToken);
        }

        public Task<WatchEventResult> SendEpisodeWatchedAsync(
            string token,
            int? seriesTvdbId,
            int? episodeTvdbId,
            int? seriesTmdbId,
            int? episodeTmdbId,
            string seriesTitle,
            string? year,
            CancellationToken cancellationToken)
        {
            return SendAsync(token, new Dictionary<string, object?>
            {
                ["type"] = "episode",
                ["seriesTvdbId"] = seriesTvdbId,
                ["episodeTvdbId"] = episodeTvdbId,
                ["seriesTmdbId"] = seriesTmdbId,
                ["episodeTmdbId"] = episodeTmdbId,
                ["seriesTitle"] = seriesTitle,
                ["year"] = year
            }, cancellationToken);
        }

        private async Task<WatchEventResult> SendAsync(string token, Dictionary<string, object?> payload, CancellationToken cancellationToken)
        {
            // Config override only honored in DEBUG (see Plugin.GetPages); Release always uses the build-time default.
#if DEBUG
            var configuredUrl = Plugin.Instance?.Configuration.ApiBaseUrl;
            var baseUrl = string.IsNullOrWhiteSpace(configuredUrl) ? PluginConfiguration.DefaultApiBaseUrl : configuredUrl;
#else
            var baseUrl = PluginConfiguration.DefaultApiBaseUrl;
#endif
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("[RecapTV] API base URL is not configured; dropping watch event");
                return WatchEventResult.Error;
            }

            baseUrl = baseUrl.TrimEnd('/');
            var nonNullPayload = payload.Where(kvp => kvp.Value is not null).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/jellyfin/webhook")
            {
                Content = JsonContent.Create(nonNullPayload, options: JsonOptions)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        _logger.LogWarning("[RecapTV] Rejected the stored token (401)");
                        return WatchEventResult.Unauthorized;
                    case HttpStatusCode.BadRequest:
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        _logger.LogWarning("[RecapTV] Rejected the watch event payload (400): {Body}", errorBody);
                        return WatchEventResult.InvalidEvent;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[RecapTV] Webhook call failed with status {Status}", response.StatusCode);
                    return WatchEventResult.Error;
                }

                var body = await response.Content.ReadFromJsonAsync<WebhookResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
                return body?.Synced == true ? WatchEventResult.Synced : WatchEventResult.Disabled;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[RecapTV] Failed to reach {BaseUrl}", baseUrl);
                return WatchEventResult.Error;
            }
        }

        private sealed class WebhookResponse
        {
            public bool Status { get; set; }

            public bool Synced { get; set; }
        }
    }
}
