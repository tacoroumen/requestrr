using System;
using System.Collections.Generic;
using System.Linq;
using Requestrr.WebApi.RequestrrBot.ChatClients.Discord;
using Requestrr.WebApi.RequestrrBot.DownloadClients;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Overseerr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Radarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Sonarr;
using Requestrr.WebApi.RequestrrBot.Locale;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    public enum MatrixCommandKind
    {
        MovieByTitle,
        MovieByTmdbId,
        TvShowByTitle,
        TvShowByTvdbId,
        MovieIssue,
        TvShowIssue,
        MusicArtist,
    }

    public class MatrixCommand
    {
        public string Name { get; set; }
        public MatrixCommandKind Kind { get; set; }
        public int CategoryId { get; set; }

        public string Usage(string prefix)
        {
            var argument = Kind switch
            {
                MatrixCommandKind.MovieByTitle => "<movie title>",
                MatrixCommandKind.MovieByTmdbId => "<tmdb id>",
                MatrixCommandKind.TvShowByTitle => "<tv show title>",
                MatrixCommandKind.TvShowByTvdbId => "<tvdb id>",
                MatrixCommandKind.MovieIssue => "<movie title>",
                MatrixCommandKind.TvShowIssue => "<tv show title>",
                MatrixCommandKind.MusicArtist => "<artist name>",
                _ => string.Empty,
            };

            return $"`{prefix}{Name} {argument}`";
        }
    }

    /// <summary>
    /// Maps text commands to request workflows. It mirrors the Discord slash commands:
    /// one command per Radarr/Sonarr/Overseerr category, or "movie"/"movie-tmdb" and "tv"/"tv-tvdb" otherwise.
    /// </summary>
    public class MatrixCommandCatalog
    {
        private const int DefaultCategoryId = 99999;
        private readonly Dictionary<string, MatrixCommand> _commands = new Dictionary<string, MatrixCommand>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<MatrixCommand> Commands => _commands.Values;

        public static MatrixCommandCatalog Build(DiscordSettings downloadSettings, RadarrSettings radarr, SonarrSettings sonarr, OverseerrSettings overseerr, LidarrSettings lidarr = null, bool movieIssues = false, bool tvShowIssues = false)
        {
            var catalog = new MatrixCommandCatalog();

            if (downloadSettings.MovieDownloadClient == DownloadClient.Radarr)
            {
                foreach (var category in radarr.Categories)
                    catalog.Add(category.Name, MatrixCommandKind.MovieByTitle, category.Id);
            }
            else if (downloadSettings.MovieDownloadClient == DownloadClient.Overseerr && overseerr.Movies.Categories.Any())
            {
                foreach (var category in overseerr.Movies.Categories)
                    catalog.Add(category.Name, MatrixCommandKind.MovieByTitle, category.Id);
            }
            else if (downloadSettings.MovieDownloadClient != DownloadClient.Disabled)
            {
                catalog.Add(Language.Current.DiscordCommandMovieRequestTitleName, MatrixCommandKind.MovieByTitle, DefaultCategoryId);
                catalog.Add(Language.Current.DiscordCommandMovieRequestTmbdName, MatrixCommandKind.MovieByTmdbId, DefaultCategoryId);
            }

            if (downloadSettings.TvShowDownloadClient == DownloadClient.Sonarr)
            {
                foreach (var category in sonarr.Categories)
                    catalog.Add(category.Name, MatrixCommandKind.TvShowByTitle, category.Id);
            }
            else if (downloadSettings.TvShowDownloadClient == DownloadClient.Overseerr && overseerr.TvShows.Categories.Any())
            {
                foreach (var category in overseerr.TvShows.Categories)
                    catalog.Add(category.Name, MatrixCommandKind.TvShowByTitle, category.Id);
            }
            else if (downloadSettings.TvShowDownloadClient != DownloadClient.Disabled)
            {
                catalog.Add(Language.Current.DiscordCommandTvRequestTitleName, MatrixCommandKind.TvShowByTitle, DefaultCategoryId);
                catalog.Add(Language.Current.DiscordCommandTvRequestTvdbName, MatrixCommandKind.TvShowByTvdbId, DefaultCategoryId);
            }

            if (downloadSettings.MusicDownloadClient == DownloadClient.Lidarr && lidarr != null)
            {
                foreach (var category in lidarr.Categories)
                    catalog.Add(category.Name, MatrixCommandKind.MusicArtist, category.Id);
            }

            if (movieIssues)
                catalog.Add($"{Language.Current.DiscordCommandIssueName}-{Language.Current.DiscordCommandMovieIssueTitleName}", MatrixCommandKind.MovieIssue, DefaultCategoryId);

            if (tvShowIssues)
                catalog.Add($"{Language.Current.DiscordCommandIssueName}-{Language.Current.DiscordCommandTvIssueTitleName}", MatrixCommandKind.TvShowIssue, DefaultCategoryId);

            return catalog;
        }

        private void Add(string name, MatrixCommandKind kind, int categoryId)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            var normalized = name.Trim().Replace(' ', '-').ToLowerInvariant();
            _commands.TryAdd(normalized, new MatrixCommand { Name = normalized, Kind = kind, CategoryId = categoryId });
        }

        public bool TryGet(string name, out MatrixCommand command)
        {
            return _commands.TryGetValue(name, out command);
        }

        public const int IssueCategoryId = DefaultCategoryId;

        public string FirstMovieCommandName()
        {
            return _commands.Values.FirstOrDefault(x => x.Kind == MatrixCommandKind.MovieByTitle)?.Name
                ?? _commands.Values.FirstOrDefault()?.Name
                ?? Language.Current.DiscordCommandMovieRequestTitleName;
        }
    }
}
