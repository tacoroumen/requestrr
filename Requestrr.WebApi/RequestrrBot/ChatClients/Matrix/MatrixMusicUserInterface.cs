using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Music;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    /// <summary>
    /// Shows artist search results and confirmations as Matrix messages, like the movie and tv show interfaces.
    /// </summary>
    public class MatrixMusicUserInterface : IMusicUserInterface
    {
        private const int MaxResults = 10;
        private readonly MatrixCommandContext _context;

        public MatrixMusicUserInterface(MatrixCommandContext context)
        {
            _context = context;
        }

        public async Task ShowMusicArtistSelection(MusicRequest request, IReadOnlyList<MusicArtist> music)
        {
            var shown = music.Take(MaxResults).ToArray();
            var choices = shown
                .Select(artist => (Func<Task>)(() => _context.Workflows.CreateMusicWorkflow(_context, request.CategoryId).HandleMusicArtistSelectionAsync(artist.ArtistId)))
                .ToArray();

            await _context.AskToChooseAsync(
                MatrixMessage.FromMarkdown(Language.Current.MatrixCommandMusicSearch),
                shown.Select(x => x.ArtistName).ToArray(),
                choices);
        }

        public async Task WarnNoMusicArtistFoundAsync(string musicName)
        {
            _context.EndConversation();
            await _context.SendAsync(Language.Current.DiscordCommandMusicArtistNotFound.ReplaceTokens(LanguageTokens.MusicArtistName, musicName));
        }

        public async Task DisplayMusicArtistDetailsAsync(MusicRequest request, MusicArtist music)
        {
            var artistId = music.ArtistId;
            await _context.AskToConfirmAsync(
                BuildDetails(music).AppendMarkdown(Language.Current.MatrixCommandMusicRequestConfirm),
                () => _context.Workflows.CreateMusicWorkflow(_context, request.CategoryId).RequestMusicArtistAsync(artistId));
        }

        public async Task DisplayArtistRequestDeniedAsync(MusicArtist music)
        {
            _context.EndConversation();
            await _context.SendAsync(Language.Current.DiscordCommandMusicArtistRequestDenied);
        }

        public async Task DisplayArtistRequestSuccessAsync(MusicArtist music)
        {
            _context.EndConversation();
            await _context.SendAsync(Language.Current.DiscordCommandMusicArtistRequestSuccess.ReplaceTokens(music));
        }

        public async Task WarnMusicArtistAlreadyAvailableAsync(MusicArtist music)
        {
            _context.EndConversation();
            await _context.SendAsync(BuildDetails(music).AppendMarkdown(Language.Current.DiscordCommandMusicArtistAlreadyAvailable));
        }

        public async Task WarnMusicArtistUnavailableAndAlreadyHasNotificationAsync(MusicArtist music)
        {
            _context.EndConversation();
            await _context.SendAsync(BuildDetails(music).AppendMarkdown(Language.Current.DiscordCommandMusicArtistRequestAlreadyExistNotified));
        }

        public async Task AskForNotificationArtistRequestAsync(MusicArtist music)
        {
            var artistId = music.ArtistId;
            await _context.AskToConfirmAsync(
                BuildDetails(music).AppendMarkdown(Language.Current.MatrixCommandMusicNotificationRequest),
                () => _context.Workflows.CreateMusicNotificationWorkflow(_context).AddNotificationArtistAsync(_context.UserId, artistId));
        }

        public async Task DisplayNotificationArtistSuccessAsync(MusicArtist music)
        {
            _context.EndConversation();
            await _context.SendAsync(Language.Current.DiscordCommandMusicArtistNotificationSuccess.ReplaceTokens(music));
        }

        private static MatrixMessage BuildDetails(MusicArtist music)
        {
            var message = new MatrixMessage().AppendHeading(music.ArtistName);

            if (!string.IsNullOrWhiteSpace(music.PosterPath) && music.PosterPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                message.ImageUrl = music.PosterPath;
            }

            if (!string.IsNullOrWhiteSpace(music.Overview))
            {
                var overview = music.Overview.Length > 300 ? music.Overview.Substring(0, 300).TrimEnd() + "..." : music.Overview;
                message.AppendLine(overview.Replace("*", string.Empty).Replace("_", string.Empty).Replace("`", string.Empty));
            }

            if (!string.IsNullOrWhiteSpace(music.Quality))
            {
                message.AppendLine($"**{Language.Current.DiscordEmbedMusicQuality}:** {music.Quality}");
            }

            message.AppendLink("MusicBrainz", $"https://musicbrainz.org/artist/{music.ArtistId}");

            if (!string.IsNullOrWhiteSpace(music.PlexUrl) && music.PlexUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                message.AppendMarkdown(" · ").AppendLink($"Plex: {Language.Current.DiscordEmbedMusicListenNow}", music.PlexUrl);
            }

            if (!string.IsNullOrWhiteSpace(music.EmbyUrl) && music.EmbyUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                message.AppendMarkdown(" · ").AppendLink($"Emby: {Language.Current.DiscordEmbedMusicListenNow}", music.EmbyUrl);
            }

            return message.AppendLine().AppendLine();
        }
    }
}
