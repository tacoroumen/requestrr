using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Movies;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    /// <summary>
    /// Shows movie search results and confirmations as Matrix messages.
    /// Requesters answer numbered lists with a number and confirmations with "yes" or ✅.
    /// </summary>
    public class MatrixMovieUserInterface : IMovieUserInterface
    {
        private const int MaxResults = 10;
        private readonly MatrixCommandContext _context;

        public MatrixMovieUserInterface(MatrixCommandContext context)
        {
            _context = context;
        }

        public async Task ShowMovieSelection(MovieRequest request, IReadOnlyList<Movie> movies)
        {
            var shown = movies.Take(MaxResults).ToArray();
            var choices = shown
                .Select(movie => (Func<Task>)(() => _context.Workflows.CreateMovieWorkflow(_context, request.CategoryId).HandleMovieSelectionAsync(int.Parse(movie.TheMovieDbId))))
                .ToArray();

            await _context.AskToChooseAsync(
                MatrixMessage.FromMarkdown(Language.Current.MatrixCommandMovieSearch),
                shown.Select(FormatTitle).ToArray(),
                choices);
        }

        public Task ShowMovieIssueSelection(MovieRequest request, IReadOnlyList<Movie> movies) => NotSupportedAsync();

        public async Task WarnNoMovieFoundAsync(string movieName)
        {
            _context.EndConversation();
            await _context.SendAsync(Language.Current.DiscordCommandMovieNotFound.ReplaceTokens(LanguageTokens.MovieTitle, movieName));
        }

        public async Task WarnNoMovieFoundByTheMovieDbIdAsync(string theMovieDbIdTextValue)
        {
            _context.EndConversation();
            await _context.SendAsync(Language.Current.DiscordCommandMovieNotFoundTMDB.ReplaceTokens(LanguageTokens.MovieTMDB, theMovieDbIdTextValue));
        }

        public async Task WarnMovieAlreadyAvailableAsync(Movie movie)
        {
            _context.EndConversation();
            await _context.SendAsync((await BuildDetailsAsync(movie)).AppendMarkdown(Language.Current.DiscordCommandMovieAlreadyAvailable));
        }

        public async Task WarnMovieAlreadyRequestedAsync(Movie movie)
        {
            _context.EndConversation();
            await _context.SendAsync((await BuildDetailsAsync(movie)).AppendMarkdown(Language.Current.DiscordCommandMovieRequestAlreadyExist));
        }

        public async Task WarnMovieUnavailableAndAlreadyHasNotificationAsync(Movie movie)
        {
            _context.EndConversation();
            await _context.SendAsync((await BuildDetailsAsync(movie)).AppendMarkdown(Language.Current.DiscordCommandMovieRequestAlreadyExistNotified));
        }

        public async Task DisplayMovieDetailsAsync(MovieRequest request, Movie movie)
        {
            var question = Language.Current.MatrixCommandMovieRequestConfirm;

            if (DateTime.TryParse(movie.ReleaseDate, out var releaseDate) && releaseDate > DateTime.UtcNow)
            {
                question = Language.Current.MatrixCommandMovieNotReleased;
            }

            var movieId = int.Parse(movie.TheMovieDbId);
            await _context.AskToConfirmAsync(
                (await BuildDetailsAsync(movie)).AppendMarkdown(question),
                () => _context.Workflows.CreateMovieWorkflow(_context, request.CategoryId).RequestMovieAsync(movieId));
        }

        public async Task AskForNotificationRequestAsync(Movie movie)
        {
            var movieId = int.Parse(movie.TheMovieDbId);
            await _context.AskToConfirmAsync(
                (await BuildDetailsAsync(movie)).AppendMarkdown(Language.Current.MatrixCommandMovieNotificationRequest),
                () => _context.Workflows.CreateMovieNotificationWorkflow(_context).AddNotificationAsync(_context.UserId, movieId));
        }

        public async Task DisplayNotificationSuccessAsync(Movie movie)
        {
            _context.EndConversation();
            await _context.SendAsync(Language.Current.DiscordCommandMovieNotificationSuccess.ReplaceTokens(movie));
        }

        public async Task DisplayRequestDeniedAsync(Movie movie)
        {
            _context.EndConversation();
            await _context.SendAsync(Language.Current.DiscordCommandMovieRequestDenied);
            await _context.Workflows.PostRequestSummaryAsync(_context, await BuildDetailsAsync(movie), Language.Current.DiscordCommandRequestDenied);
        }

        public async Task DisplayRequestSuccessAsync(Movie movie)
        {
            _context.EndConversation();
            await _context.SendAsync(Language.Current.DiscordCommandMovieRequestSuccess.ReplaceTokens(movie));
            await _context.Workflows.PostRequestSummaryAsync(_context, await BuildDetailsAsync(movie), Language.Current.DiscordCommandRequestApproved);
        }

        public async Task DisplayRequestPendingAsync(Movie movie, int requestId)
        {
            _context.EndConversation();
            var eventId = await _context.SendAsync(Language.Current.DiscordCommandMovieRequestPendingSilent.ReplaceTokens(movie));
            await _context.Workflows.TrackPendingRequestAsync(_context, requestId, eventId, movie.Title, await BuildDetailsAsync(movie));
        }

        public Task DisplayMovieIssueDetailsAsync(MovieRequest request, Movie movie, string issue) => NotSupportedAsync();
        public Task DisplayMovieIssueModalAsync(MovieRequest request, Movie movie, string issue) => NotSupportedAsync();
        public Task CompleteMovieIssueModalRequestAsync(Movie movie, bool success) => NotSupportedAsync();

        private async Task NotSupportedAsync()
        {
            _context.EndConversation();
            await _context.SendAsync(Language.Current.MatrixNotSupported);
        }

        private static string FormatTitle(Movie movie)
        {
            var year = !string.IsNullOrWhiteSpace(movie.ReleaseDate) && movie.ReleaseDate.Length >= 4 ? movie.ReleaseDate.Substring(0, 4) : null;
            return year != null ? $"{movie.Title} ({year})" : movie.Title;
        }

        private async Task<MatrixMessage> BuildDetailsAsync(Movie movie)
        {
            var message = new MatrixMessage().AppendHeading(FormatTitle(movie));
            message.ImageUrl = movie.PosterPath;

            if (!string.IsNullOrWhiteSpace(movie.Overview))
            {
                var overview = movie.Overview.Length > 300 ? movie.Overview.Substring(0, 300).TrimEnd() + "..." : movie.Overview;
                message.AppendLine(overview.Replace("*", string.Empty).Replace("_", string.Empty).Replace("`", string.Empty));
            }

            var facts = new List<string>();

            if (!string.IsNullOrWhiteSpace(movie.Quality))
            {
                facts.Add($"**{Language.Current.DiscordEmbedMovieQuality}:** {movie.Quality}p");
            }

            try
            {
                var details = await _context.Workflows.GetMovieSearcher().GetMovieDetails(new MovieRequest(null, int.MinValue), movie.TheMovieDbId);

                if (!string.IsNullOrWhiteSpace(details.InTheatersDate))
                {
                    facts.Add($"**{Language.Current.DiscordEmbedMovieInTheaters}:** {details.InTheatersDate}");
                }
                else if (DateTime.TryParse(movie.ReleaseDate, out var releaseDate))
                {
                    facts.Add($"**{Language.Current.DiscordEmbedMovieInTheaters}:** {releaseDate.ToString("MMMM dd yyyy", DateTimeFormatInfo.InvariantInfo)}");
                }

                if (!string.IsNullOrWhiteSpace(details.PhysicalReleaseName) && !string.IsNullOrWhiteSpace(details.PhysicalReleaseDate))
                {
                    facts.Add($"**{details.PhysicalReleaseName} {Language.Current.DiscordEmbedMovieRelease}:** {details.PhysicalReleaseDate}");
                }
            }
            catch
            {
                // Details are optional; show what the search returned.
            }

            foreach (var fact in facts)
            {
                message.AppendLine(fact);
            }

            message.AppendLink("TMDB", $"https://www.themoviedb.org/movie/{movie.TheMovieDbId}");
            AppendWatchLink(message, "Plex", movie.PlexUrl);
            AppendWatchLink(message, "Emby", movie.EmbyUrl);
            AppendWatchLink(message, "Media", movie.MediaUrl);

            return message.AppendLine().AppendLine();
        }

        private static void AppendWatchLink(MatrixMessage message, string name, string url)
        {
            if (!string.IsNullOrWhiteSpace(url) && url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                message.AppendMarkdown(" · ").AppendLink($"{name}: {Language.Current.DiscordEmbedMovieWatchNow}", url);
            }
        }
    }
}
