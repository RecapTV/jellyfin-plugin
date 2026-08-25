using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.RecapTV.Services;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.RecapTV.Notifiers
{
    /// <summary>
    /// Fires on every completed-or-not playback stop, for every user watching that session.
    /// Sends whichever of TMDB/TVDB ids Jellyfin has for the item (and, for episodes, its
    /// series) - only items with at least one usable id pair and a connected RecapTV token
    /// do anything.
    /// </summary>
    public class PlaybackStopNotifier : IEventConsumer<PlaybackStopEventArgs>
    {
        private readonly TokenStore _tokenStore;
        private readonly RecapTVApiClient _apiClient;
        private readonly ILogger<PlaybackStopNotifier> _logger;

        public PlaybackStopNotifier(TokenStore tokenStore, RecapTVApiClient apiClient, ILogger<PlaybackStopNotifier> logger)
        {
            _tokenStore = tokenStore;
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task OnEvent(PlaybackStopEventArgs eventArgs)
        {
            if (!eventArgs.PlayedToCompletion || eventArgs.Item is null || eventArgs.Users.Count == 0)
            {
                _logger.LogInformation(
                    "[RecapTV] Ignoring playback stop: playedToCompletion={PlayedToCompletion}, item={Item}, users={Users}",
                    eventArgs.PlayedToCompletion,
                    eventArgs.Item?.Name,
                    eventArgs.Users.Count);
                return;
            }

            foreach (var user in eventArgs.Users)
            {
                var record = _tokenStore.Get(user.Id);
                if (record is null)
                {
                    _logger.LogInformation("[RecapTV] Skipping user {UserId}: no RecapTV token stored", user.Id);
                    continue;
                }

                var result = eventArgs.Item switch
                {
                    Episode episode => await SendEpisodeAsync(record.Token, episode).ConfigureAwait(false),
                    Movie movie => await SendMovieAsync(record.Token, movie).ConfigureAwait(false),
                    _ => (WatchEventResult?)null
                };

                _logger.LogInformation("[RecapTV] Watch event result for user {UserId}, item {Item}: {Result}", user.Id, eventArgs.Item.Name, result);

                if (result == WatchEventResult.Unauthorized)
                {
                    _logger.LogWarning("[RecapTV] Token for user {UserId} was rejected; marking invalid", user.Id);
                    _tokenStore.MarkInvalid(user.Id, "RecapTV rejected the token. Reconnect from your profile menu.");
                }
            }
        }

        private static int? TryGetProviderId(Dictionary<string, string> providerIds, string key)
        {
            return providerIds.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed) ? parsed : null;
        }

        private Task<WatchEventResult> SendMovieAsync(string token, Movie movie)
        {
            int? tmdbId = TryGetProviderId(movie.ProviderIds, "Tmdb");
            int? tvdbId = TryGetProviderId(movie.ProviderIds, "Tvdb");

            if (tmdbId is null && tvdbId is null)
            {
                _logger.LogInformation("[RecapTV] Skipping movie {Name}: no TMDB or TVDB id", movie.Name);
                return Task.FromResult(WatchEventResult.InvalidEvent);
            }

            _logger.LogInformation("[RecapTV] Sending movie {Name} (tmdbId={TmdbId}, tvdbId={TvdbId}, year={Year})", movie.Name, tmdbId, tvdbId, movie.ProductionYear);
            return _apiClient.SendMovieWatchedAsync(token, tmdbId, tvdbId, movie.Name, movie.ProductionYear?.ToString(), CancellationToken.None);
        }

        private Task<WatchEventResult> SendEpisodeAsync(string token, Episode episode)
        {
            var series = episode.Series;
            if (series is null)
            {
                _logger.LogInformation("[RecapTV] Skipping episode {Name}: no series", episode.Name);
                return Task.FromResult(WatchEventResult.InvalidEvent);
            }

            int? seriesTvdbId = TryGetProviderId(series.ProviderIds, "Tvdb");
            int? episodeTvdbId = TryGetProviderId(episode.ProviderIds, "Tvdb");
            int? seriesTmdbId = TryGetProviderId(series.ProviderIds, "Tmdb");
            int? episodeTmdbId = TryGetProviderId(episode.ProviderIds, "Tmdb");

            var haveTvdbPair = seriesTvdbId is not null && episodeTvdbId is not null;
            var haveTmdbPair = seriesTmdbId is not null && episodeTmdbId is not null;
            if (!haveTvdbPair && !haveTmdbPair)
            {
                _logger.LogInformation("[RecapTV] Skipping episode {Name}: missing series/episode id pair on both TVDB and TMDB", episode.Name);
                return Task.FromResult(WatchEventResult.InvalidEvent);
            }

            var seriesTitle = series.Name ?? episode.SeriesName;
            var sendSeriesTvdbId = haveTvdbPair ? seriesTvdbId : null;
            var sendEpisodeTvdbId = haveTvdbPair ? episodeTvdbId : null;
            var sendSeriesTmdbId = haveTmdbPair ? seriesTmdbId : null;
            var sendEpisodeTmdbId = haveTmdbPair ? episodeTmdbId : null;

            _logger.LogInformation(
                "[RecapTV] Sending episode {Name} of {Series} (seriesTvdbId={SeriesTvdbId}, episodeTvdbId={EpisodeTvdbId}, seriesTmdbId={SeriesTmdbId}, episodeTmdbId={EpisodeTmdbId}, year={Year})",
                episode.Name,
                seriesTitle,
                sendSeriesTvdbId,
                sendEpisodeTvdbId,
                sendSeriesTmdbId,
                sendEpisodeTmdbId,
                series.ProductionYear);

            return _apiClient.SendEpisodeWatchedAsync(
                token,
                sendSeriesTvdbId,
                sendEpisodeTvdbId,
                sendSeriesTmdbId,
                sendEpisodeTmdbId,
                seriesTitle,
                series.ProductionYear?.ToString(),
                CancellationToken.None);
        }
    }
}
