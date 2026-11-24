using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.TvShows;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    /// <summary>
    /// Shows tv show search results, season pickers and confirmations as Matrix messages.
    /// </summary>
    public class MatrixTvShowUserInterface : ITvShowUserInterface
    {
        private const int MaxResults = 10;
        private readonly MatrixCommandContext _context;

        public MatrixTvShowUserInterface(MatrixCommandContext context)
        {
            _context = context;
        }

        public async Task ShowTvShowSelection(TvShowRequest request, IReadOnlyList<SearchedTvShow> searchedTvShows)
        {
            var shown = searchedTvShows.Take(MaxResults).ToArray();
            var choices = shown
                .Select(show => (Func<Task>)(() => _context.Workflows.CreateTvShowWorkflow(_context, request.CategoryId).HandleTvShowSelectionAsync(show.TheTvDbId)))
                .ToArray();

            await _context.AskToChooseAsync(
                MatrixMessage.FromMarkdown(Language.Current.MatrixCommandTvSearch),
                shown.Select(FormatTitle).ToArray(),
                choices);
        }

        public async Task DisplayMultiSeasonSelectionAsync(TvShowRequest request, TvShow tvShow, TvSeason[] tvSeasons)
        {
            var choices = tvSeasons
                .Select(season => (Func<Task>)(() => _context.Workflows.CreateTvShowWorkflow(_context, request.CategoryId).HandleSeasonSelectionAsync(tvShow.TheTvDbId, season.SeasonNumber)))
                .ToArray();

            await _context.AskToChooseAsync(
                BuildDetails(tvShow).AppendMarkdown(Language.Current.MatrixCommandTvSeasons),
                tvSeasons.Select(season => FormatSeasonName(tvShow, season)).ToArray(),
                choices);
        }

        public async Task DisplayTvShowDetailsForSeasonAsync(TvShowRequest request, TvShow tvShow, TvSeason season)
        {
            var question = season is AllTvSeasons
                ? Language.Current.MatrixCommandTvRequestConfirmAllSeasons
                : season is FutureTvSeasons
                    ? Language.Current.MatrixCommandTvRequestConfirmFutureSeasons
                    : Language.Current.MatrixCommandTvRequestConfirmSeason.ReplaceTokens(LanguageTokens.SeasonNumber, season.SeasonNumber.ToString());

            var seasonNumber = season.SeasonNumber;
            await _context.AskToConfirmAsync(
                BuildDetails(tvShow).AppendMarkdown(question),
                () => _context.Workflows.CreateTvShowWorkflow(_context, request.CategoryId).RequestSeasonSelectionAsync(tvShow.TheTvDbId, seasonNumber));
        }

        public async Task AskForSeasonNotificationRequestAsync(TvShow tvShow, TvSeason selectedSeason)
        {
            var question = Language.Current.MatrixCommandTvNotificationRequestSeason.ReplaceTokens(tvShow, selectedSeason.SeasonNumber);

            if (selectedSeason is FutureTvSeasons)
            {
                question = tvShow.AllSeasonsAvailable()
                    ? Language.Current.MatrixCommandTvNotificationRequestFutureSeasonAvailable
                    : tvShow.AllSeasonsFullyRequested()
                        ? Language.Current.MatrixCommandTvNotificationRequestFutureSeasonRequested
                        : Language.Current.MatrixCommandTvNotificationRequestFutureSeasonMissing;
            }

            var seasonType = selectedSeason.GetType().Name.First().ToString();
            var seasonNumber = selectedSeason.SeasonNumber;

            await _context.AskToConfirmAsync(
                BuildDetails(tvShow).AppendMarkdown(question),
                () => _context.Workflows.CreateTvShowNotificationWorkflow(_context).AddNotificationAsync(_context.UserId, tvShow.TheTvDbId, seasonType, seasonNumber));
        }

        public async Task DisplayNotificationSuccessForSeasonAsync(TvShow tvShow, TvSeason requestedSeason)
        {
            var message = requestedSeason is FutureTvSeasons
                ? Language.Current.DiscordCommandTvNotificationSuccessFutureSeasons.ReplaceTokens(tvShow)
                : Language.Current.DiscordCommandTvNotificationSuccessSeason.ReplaceTokens(tvShow, requestedSeason.SeasonNumber);

            await EndWithAsync(message);
        }

        public Task DisplayRequestDeniedForSeasonAsync(TvShow tvShow, TvSeason selectedSeason)
        {
            return EndWithAsync(Language.Current.DiscordCommandTvRequestDenied);
        }

        public Task DisplayRequestSuccessForSeasonAsync(TvShow tvShow, TvSeason requestedSeason)
        {
            var message = requestedSeason is AllTvSeasons
                ? Language.Current.DiscordCommandTvRequestSuccessAllSeasons.ReplaceTokens(tvShow, requestedSeason.SeasonNumber)
                : requestedSeason is FutureTvSeasons
                    ? Language.Current.DiscordCommandTvRequestSuccessFutureSeasons.ReplaceTokens(tvShow, requestedSeason.SeasonNumber)
                    : Language.Current.DiscordCommandTvRequestSuccessSeason.ReplaceTokens(tvShow, requestedSeason.SeasonNumber);

            return EndWithAsync(message);
        }

        public async Task DisplayRequestPendingForSeasonAsync(TvShow tvShow, TvSeason requestedSeason, int requestId)
        {
            var message = requestedSeason is AllTvSeasons
                ? Language.Current.DiscordCommandTvRequestPendingAllSeasonsSilent.ReplaceTokens(tvShow, requestedSeason.SeasonNumber)
                : requestedSeason is FutureTvSeasons
                    ? Language.Current.DiscordCommandTvRequestPendingFutureSeasonsSilent.ReplaceTokens(tvShow, requestedSeason.SeasonNumber)
                    : Language.Current.DiscordCommandTvRequestPendingSeasonSilent.ReplaceTokens(tvShow, requestedSeason.SeasonNumber);

            _context.EndConversation();
            var eventId = await _context.SendAsync(message);
            _context.Workflows.TrackPendingRequest(requestId, _context.RoomId, eventId, tvShow.Title);
        }

        public Task WarnAlreadyNotifiedForSeasonsAsync(TvShow tvShow, TvSeason requestedSeason)
        {
            var message = Language.Current.DiscordCommandTvRequestAlreadyExistNotifiedSeason.ReplaceTokens(tvShow, requestedSeason.SeasonNumber);

            if (requestedSeason is FutureTvSeasons)
            {
                message = tvShow.AllSeasonsAvailable()
                    ? Language.Current.DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonAvailable
                    : tvShow.AllSeasonsFullyRequested()
                        ? Language.Current.DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonRequested
                        : Language.Current.DiscordCommandTvRequestAlreadyExistNotifiedFutureSeasonFound;
            }

            return EndWithDetailsAsync(tvShow, message);
        }

        public Task WarnAlreadySeasonAlreadyRequestedAsync(TvShow tvShow, TvSeason requestedSeason)
        {
            var message = Language.Current.DiscordCommandTvRequestAlreadyExistSeason.ReplaceTokens(tvShow, requestedSeason.SeasonNumber);

            if (requestedSeason is FutureTvSeasons)
            {
                message = tvShow.AllSeasonsAvailable()
                    ? Language.Current.DiscordCommandTvRequestAlreadyExistFutureSeasonAvailable
                    : tvShow.AllSeasonsFullyRequested()
                        ? Language.Current.DiscordCommandTvRequestAlreadyExistFutureSeasonRequested
                        : Language.Current.DiscordCommandTvRequestAlreadyExistFutureSeasonFound;
            }

            return EndWithDetailsAsync(tvShow, message);
        }

        public Task WarnNoTvShowFoundAsync(string tvShowName)
        {
            return EndWithAsync(Language.Current.DiscordCommandTvNotFound.ReplaceTokens(LanguageTokens.TvShowTitle, tvShowName));
        }

        public Task WarnNoTvShowFoundByTvDbIdAsync(int tvDbId)
        {
            return EndWithAsync(Language.Current.DiscordCommandTvNotFoundTVDBID.ReplaceTokens(LanguageTokens.TvShowTVDBID, tvDbId.ToString()));
        }

        public Task WarnSeasonAlreadyAvailableAsync(TvShow tvShow, TvSeason selectedSeason)
        {
            return EndWithDetailsAsync(tvShow, Language.Current.DiscordCommandTvRequestAlreadyAvailableSeason.ReplaceTokens(tvShow, selectedSeason.SeasonNumber));
        }

        public Task WarnShowCannotBeRequestedAsync(TvShow tvShow)
        {
            return EndWithDetailsAsync(tvShow, Language.Current.DiscordCommandTvRequestUnsupported);
        }

        public Task WarnShowHasEndedAsync(TvShow tvShow)
        {
            var message = tvShow.AllSeasonsAvailable()
                ? Language.Current.DiscordCommandTvRequestAlreadyAvailableSeries
                : Language.Current.DiscordCommandTvRequestAlreadyExistSeries;

            return EndWithDetailsAsync(tvShow, message.ReplaceTokens(tvShow));
        }

        public Task ShowTvShowIssueSelection(TvShowRequest request, IReadOnlyList<SearchedTvShow> searchedTvShows) => EndWithAsync(Language.Current.MatrixNotSupported);
        public Task DisplayTvShowIssueDetailsAsync(TvShowRequest request, TvShow tvShow, string issue, int? seasonNumber, int? episodeNumber) => EndWithAsync(Language.Current.MatrixNotSupported);
        public Task DisplayTvShowIssueModalAsync(TvShowRequest request, TvShow tvShow, string issue, int? seasonNumber, int? episodeNumber) => EndWithAsync(Language.Current.MatrixNotSupported);
        public Task CompleteTvShowIssueModalRequestAsync(TvShow tvShow, bool success) => EndWithAsync(Language.Current.MatrixNotSupported);

        private async Task EndWithAsync(string markdown)
        {
            _context.EndConversation();
            await _context.SendAsync(markdown);
        }

        private async Task EndWithDetailsAsync(TvShow tvShow, string markdown)
        {
            _context.EndConversation();
            await _context.SendAsync(BuildDetails(tvShow).AppendMarkdown(markdown));
        }

        private static string FormatTitle(SearchedTvShow show)
        {
            var year = !string.IsNullOrWhiteSpace(show.FirstAired) && show.FirstAired.Length >= 4 ? show.FirstAired.Substring(0, 4) : null;
            return year != null ? $"{show.Title} ({year})" : show.Title;
        }

        private static string FormatSeasonName(TvShow tvShow, TvSeason season)
        {
            var seasonName = season is AllTvSeasons
                ? Language.Current.DiscordEmbedTvAllSeasons
                : season is FutureTvSeasons
                    ? Language.Current.DiscordEmbedTvFutureSeasons
                    : Language.Current.DiscordEmbedTvSeason.ReplaceTokens(LanguageTokens.SeasonNumber, season.SeasonNumber.ToString());

            if (season is AllTvSeasons)
            {
                return tvShow.AllSeasonsFullyRequested() ? $"{seasonName} ({Language.Current.DiscordEmbedTvFullyRequested})" : seasonName;
            }

            return season.IsRequested == RequestedState.Full
                ? $"{seasonName} ({Language.Current.DiscordEmbedTvFullyRequested})"
                : season.IsRequested == RequestedState.Partial
                    ? $"{seasonName} ({Language.Current.DiscordEmbedTvPartiallyRequested})"
                    : seasonName;
        }

        private static MatrixMessage BuildDetails(TvShow tvShow)
        {
            var year = !string.IsNullOrWhiteSpace(tvShow.FirstAired) && tvShow.FirstAired.Length >= 4 ? $" ({tvShow.FirstAired.Substring(0, 4)})" : string.Empty;
            var message = new MatrixMessage().AppendHeading(tvShow.Title + year);

            if (!string.IsNullOrWhiteSpace(tvShow.Overview))
            {
                var overview = tvShow.Overview.Length > 300 ? tvShow.Overview.Substring(0, 300).TrimEnd() + "..." : tvShow.Overview;
                message.AppendLine(overview.Replace("*", string.Empty).Replace("_", string.Empty).Replace("`", string.Empty));
            }

            if (!string.IsNullOrWhiteSpace(tvShow.Network)) message.AppendLine($"**{Language.Current.DiscordEmbedTvNetwork}:** {tvShow.Network}");
            if (!string.IsNullOrWhiteSpace(tvShow.Status)) message.AppendLine($"**{Language.Current.DiscordEmbedTvStatus}:** {tvShow.Status}");
            if (!string.IsNullOrWhiteSpace(tvShow.Quality)) message.AppendLine($"**{Language.Current.DiscordEmbedTvQuality}:** {tvShow.Quality}p");

            var links = new List<(string Name, string Url)>
            {
                ("Website", tvShow.WebsiteUrl),
                ($"Plex: {Language.Current.DiscordEmbedTvWatchNow}", tvShow.PlexUrl),
                ($"Emby: {Language.Current.DiscordEmbedTvWatchNow}", tvShow.EmbyUrl),
                ($"Media: {Language.Current.DiscordEmbedTvWatchNow}", tvShow.MediaUrl),
            }.Where(x => !string.IsNullOrWhiteSpace(x.Url) && x.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)).ToArray();

            for (var i = 0; i < links.Length; i++)
            {
                if (i > 0) message.AppendMarkdown(" · ");
                message.AppendLink(links[i].Name, links[i].Url);
            }

            return message.AppendLine().AppendLine();
        }
    }
}
