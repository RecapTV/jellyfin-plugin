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
    /// Movies send whichever of TMDB/TVDB ids Jellyfin has (stock movie metadata usually only
    /// sets Tmdb, not Tvdb - TheTVDB only matches TV series/episodes by default); episodes need
    /// TVDB ids on both the episode and its series. Only items with at least one usable id and a
    /// connected RecapTV token do anything.
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
                return;
            }

            foreach (var user in eventArgs.Users)
            {
                var record = _tokenStore.Get(user.Id);
                if (record is null)
                {
                    continue;
                }

                var result = eventArgs.Item switch
                {
                    Episode episode => await SendEpisodeAsync(record.Token, episode).ConfigureAwait(false),
                    Movie movie => await SendMovieAsync(record.Token, movie).ConfigureAwait(false),
                    _ => (WatchEventResult?)null
                };

                if (result == WatchEventResult.Unauthorized)
                {
                    _tokenStore.MarkInvalid(user.Id, "RecapTV rejected the token. Reconnect from your profile menu.");
                }
            }
        }

        private Task<WatchEventResult> SendMovieAsync(string token, Movie movie)
        {
            int? tmdbId = movie.ProviderIds.TryGetValue("Tmdb", out var rawTmdb) && int.TryParse(rawTmdb, out var parsedTmdb)
                ? parsedTmdb
                : null;
            int? tvdbId = movie.ProviderIds.TryGetValue("Tvdb", out var rawTvdb) && int.TryParse(rawTvdb, out var parsedTvdb)
                ? parsedTvdb
                : null;

            if (tmdbId is null && tvdbId is null)
            {
                _logger.LogDebug("Skipping movie {Name}: no TMDB or TVDB id", movie.Name);
                return Task.FromResult(WatchEventResult.InvalidEvent);
            }

            return _apiClient.SendMovieWatchedAsync(token, tmdbId, tvdbId, movie.Name, movie.ProductionYear?.ToString(), CancellationToken.None);
        }

        private Task<WatchEventResult> SendEpisodeAsync(string token, Episode episode)
        {
            var series = episode.Series;
            if (series is null
                || !series.ProviderIds.TryGetValue("Tvdb", out var rawSeries) || !int.TryParse(rawSeries, out var seriesTvdbId)
                || !episode.ProviderIds.TryGetValue("Tvdb", out var rawEpisode) || !int.TryParse(rawEpisode, out var episodeTvdbId))
            {
                _logger.LogDebug("Skipping episode {Name}: missing series/episode TVDB id", episode.Name);
                return Task.FromResult(WatchEventResult.InvalidEvent);
            }

            var seriesTitle = series.Name ?? episode.SeriesName;
            return _apiClient.SendEpisodeWatchedAsync(token, seriesTvdbId, episodeTvdbId, seriesTitle, series.ProductionYear?.ToString(), CancellationToken.None);
        }
    }
}
