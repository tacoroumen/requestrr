using DSharpPlus;
using DSharpPlus.Entities;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Requestrr.WebApi.RequestrrBot.ChatClients.Discord
{
    public class DiscordMusicUserInterface : IMusicUserInterface
    {
        private readonly DiscordInteraction _interactionContext;
        private readonly IMusicSearcher _musicSearcher;

        public DiscordMusicUserInterface(
            DiscordInteraction interactionContext,
            IMusicSearcher musicSearcher)
        {
            _interactionContext = interactionContext;
            _musicSearcher = musicSearcher;
        }


        public async Task ShowMusicArtistSelection(MusicRequest request, IReadOnlyList<MusicArtist> music)
        {
            List<DiscordSelectComponentOption> options = music.Take(15).Select(x => new DiscordSelectComponentOption(GetFormattedMusicArtistName(x), $"{request.CategoryId}/{x.ArtistId}")).ToList();
            DiscordSelectComponent select = new DiscordSelectComponent($"MuRSA/{_interactionContext.User.Id}/{request.CategoryId}", LimitStringSize(Language.Current.DiscordCommandMusicArtistRequestHelpDropdown), options);

            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddComponents(select).WithContent(Language.Current.DiscordCommandMusicArtistRequestHelp));
        }

        public async Task ShowMusicAlbumSelection(MusicRequest request, MusicArtist musicArtist, IReadOnlyList<MusicAlbum> albums, int page)
        {
            const int pageSize = 15;
            int totalPages = (int)Math.Ceiling(albums.Count / (double)pageSize);
            int currentPage = Math.Max(0, Math.Min(page, Math.Max(totalPages - 1, 0)));

            var options = new List<DiscordSelectComponentOption>
            {
                new DiscordSelectComponentOption(Language.Current.DiscordCommandMusicAlbumOptionAll, $"{request.CategoryId}/{musicArtist.ArtistId}/all")
            };

            options.AddRange(albums
                .Skip(currentPage * pageSize)
                .Take(pageSize)
                .Select(x => new DiscordSelectComponentOption(GetFormattedMusicAlbumName(x), $"{request.CategoryId}/{musicArtist.ArtistId}/{x.AlbumId}")));

            string placeholder = totalPages > 1
                ? $"{Language.Current.DiscordCommandMusicAlbumRequestHelpDropdown} ({currentPage + 1}/{totalPages})"
                : Language.Current.DiscordCommandMusicAlbumRequestHelpDropdown;

            DiscordSelectComponent select = new DiscordSelectComponent($"MuRLA/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}/{currentPage}", LimitStringSize(placeholder), options);

            var builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)), false)).AddComponents(select).WithContent(Language.Current.DiscordCommandMusicAlbumRequestHelp);

            if (totalPages > 1)
            {
                var prevButton = new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"MuRLP/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}/{Math.Max(currentPage - 1, 0)}",
                    "◀",
                    currentPage == 0
                );
                var nextButton = new DiscordButtonComponent(
                    ButtonStyle.Secondary,
                    $"MuRLP/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}/{Math.Min(currentPage + 1, totalPages - 1)}",
                    "▶",
                    currentPage >= totalPages - 1
                );

                builder.AddComponents(prevButton, nextButton);
            }

            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task DisplayMusicArtistDetailsAsync(MusicRequest request, MusicArtist musicArtist)
        {
            string message = Language.Current.DiscordCommandMusicArtistRequestConfirm;
            DiscordButtonComponent requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MuRCA/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}", Language.Current.DiscordCommandRequestButton);

            var builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(requestButton).WithContent(message);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayMusicAlbumDetailsAsync(MusicRequest request, MusicArtist artist, MusicAlbum album)
        {
            string message = Language.Current.DiscordCommandMusicAlbumRequestConfirm.ReplaceTokens(album, artist);
            DiscordButtonComponent requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MuRLC/{_interactionContext.User.Id}/{request.CategoryId}/{artist.ArtistId}/{album.AlbumId}", Language.Current.DiscordCommandRequestButton);

            var builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(artist, album)), true, album.AlbumId)).AddComponents(requestButton).WithContent(message);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task WarnMusicArtistAlreadyAvailableAsync(MusicArtist musicArtist)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{musicArtist.ArtistId}", Language.Current.DiscordCommandAvailableButton, true);
            var builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicArtistAlreadyAvailable);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task WarnMusicAlbumAlreadyAvailableAsync(MusicArtist artist, MusicAlbum album)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{artist.ArtistId}", Language.Current.DiscordCommandAvailableButton, true);
            var builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(artist, album)), true, album.AlbumId)).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicAlbumAlreadyAvailable.ReplaceTokens(album, artist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task WarnMusicAlbumAlreadyRequestedAsync(MusicArtist artist, MusicAlbum album)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{artist.ArtistId}", Language.Current.DiscordCommandRequestedButton, true);
            var builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(artist, album)), true, album.AlbumId)).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicAlbumRequestAlreadyExist.ReplaceTokens(album, artist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task WarnNoMusicArtistFoundAsync(string musicArtistName)
        {
            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent(Language.Current.DiscordCommandMusicArtistNotFound.ReplaceTokens(LanguageTokens.MusicArtistName, musicArtistName)));
        }



        public static DiscordEmbed GenerateMusicArtistDetails(MusicArtist musicArtist)
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle(musicArtist.ArtistName)
                .WithTimestamp(DateTime.Now)
                .WithUrl($"https://musicbrainz.org/artist/{musicArtist.ArtistId}")
                .WithFooter("Powered by Requestrr");

            if (!string.IsNullOrWhiteSpace(musicArtist.Overview))
                embedBuilder.WithDescription(musicArtist.Overview.Substring(0, Math.Min(musicArtist.Overview.Length, 255)) + "(...)");

            if (!string.IsNullOrWhiteSpace(musicArtist.PosterPath) && musicArtist.PosterPath.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                embedBuilder.WithImageUrl(musicArtist.PosterPath);

            if (!string.IsNullOrWhiteSpace(musicArtist.Quality))
                embedBuilder.AddField($"__{Language.Current.DiscordEmbedMusicQuality}__", $"{musicArtist.Quality}", true);

            if (!string.IsNullOrWhiteSpace(musicArtist.PlexUrl))
                embedBuilder.AddField($"__Plex__", $"[{Language.Current.DiscordEmbedMusicListenNow}]({musicArtist.PlexUrl})", true);

            if (!string.IsNullOrWhiteSpace(musicArtist.EmbyUrl))
                embedBuilder.AddField($"__Emby__", $"[{Language.Current.DiscordEmbedMusicListenNow}]({musicArtist.EmbyUrl})", true);

            return embedBuilder.Build();
        }

        public static DiscordEmbed GenerateMusicAlbumDetails(MusicArtist artist, MusicAlbum album)
        {
            var title = album.AlbumTitle;
            if (album.ReleaseDate.HasValue && !title.Contains(album.ReleaseDate.Value.Year.ToString(), StringComparison.InvariantCultureIgnoreCase))
                title = $"{title} ({album.ReleaseDate.Value.Year})";

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"{title} — {artist.ArtistName}")
                .WithTimestamp(DateTime.Now)
                .WithFooter("Powered by Requestrr");

            if (!string.IsNullOrWhiteSpace(album.AlbumId))
                embedBuilder.WithUrl($"https://musicbrainz.org/release-group/{album.AlbumId}");

            if (!string.IsNullOrWhiteSpace(album.Overview))
                embedBuilder.WithDescription(album.Overview.Substring(0, Math.Min(album.Overview.Length, 255)) + "(...)");

            if (!string.IsNullOrWhiteSpace(album.PosterPath) && album.PosterPath.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                embedBuilder.WithImageUrl(album.PosterPath);

            return embedBuilder.Build();
        }


        public async Task DisplayArtistRequestSuccessAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandRequestButtonSuccess);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMusicArtistRequestSuccess.ReplaceTokens(musicArtist));            
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayMusicAlbumRequestSuccessAsync(MusicArtist artist, MusicAlbum album)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandRequestButtonSuccess);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(artist, album)), true, album.AlbumId)).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMusicAlbumRequestSuccess.ReplaceTokens(album, artist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }



        public async Task DisplayArtistRequestDeniedAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent deniedButton = new DiscordButtonComponent(ButtonStyle.Danger, $"0/1/0", Language.Current.DiscordCommandRequestButtonDenied);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(deniedButton).WithContent(Language.Current.DiscordCommandMusicArtistRequestDenied);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }

        public async Task DisplayMusicAlbumRequestDeniedAsync(MusicArtist artist, MusicAlbum album)
        {
            DiscordButtonComponent deniedButton = new DiscordButtonComponent(ButtonStyle.Danger, $"0/1/0", Language.Current.DiscordCommandRequestButtonDenied);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(artist, album)), true, album.AlbumId)).AddComponents(deniedButton).WithContent(Language.Current.DiscordCommandMusicAlbumRequestDenied.ReplaceTokens(album, artist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }



        public async Task WarnMusicArtistUnavailableAndAlreadyHasNotificationAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{musicArtist.ArtistId}", Language.Current.DiscordCommandRequestButton, true);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicArtistRequestAlreadyExistNotified);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }



        public async Task AskForNotificationArtistRequestAsync(MusicArtist musicArtist)
        {
            var notificationButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MuNR/{_interactionContext.User.Id}/{musicArtist.ArtistId}", Language.Current.DiscordCommandRequestButton, false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("🔔")));
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(notificationButton).WithContent(Language.Current.DiscordCommandMusicArtistNotificationRequest);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task DisplayNotificationArtistSuccessAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandNotifyMeSuccess);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMusicArtistNotificationSuccess.ReplaceTokens(musicArtist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }





        private string GetFormattedMusicArtistName(MusicArtist music)
        {
            return LimitStringSize(music.ArtistName);
        }

        private string GetFormattedMusicAlbumName(MusicAlbum album)
        {
            return LimitStringSize(album.AlbumTitle);
        }
        private string LimitStringSize(string value, int limit = 100)
        {
            return value.Count() > limit ? value[..(limit - 3)] + "..." : value;
        }


        private async Task<DiscordWebhookBuilder> AddPreviousDropdownsAsync(MusicArtist music, DiscordWebhookBuilder builder, bool includeAlbumSelector = true, string selectedAlbumId = null)
        {
            DiscordSelectComponent previousMusicSelector = (await _interactionContext.GetOriginalResponseAsync()).FilterComponents<DiscordSelectComponent>().FirstOrDefault(x => x.CustomId.StartsWith("MuRSA", true, null));
            if (previousMusicSelector != null)
            {
                DiscordSelectComponent musicSelector = new DiscordSelectComponent(previousMusicSelector.CustomId, GetFormattedMusicArtistName(music), previousMusicSelector.Options);
                builder.AddComponents(musicSelector);
            }

            if (includeAlbumSelector)
            {
                DiscordSelectComponent previousAlbumSelector = (await _interactionContext.GetOriginalResponseAsync()).FilterComponents<DiscordSelectComponent>().FirstOrDefault(x => x.CustomId.StartsWith("MuRLA", true, null));
                if (previousAlbumSelector != null)
                {
                    IReadOnlyList<DiscordSelectComponentOption> albumOptions = previousAlbumSelector.Options;
                    string placeholder = string.IsNullOrWhiteSpace(previousAlbumSelector.Placeholder)
                        ? Language.Current.DiscordCommandMusicAlbumRequestHelpDropdown
                        : previousAlbumSelector.Placeholder;

                    if (!string.IsNullOrWhiteSpace(selectedAlbumId))
                    {
                        albumOptions = previousAlbumSelector.Options
                            .Select(x =>
                            {
                                bool isSelected = x.Value.EndsWith($"/{selectedAlbumId}", StringComparison.InvariantCultureIgnoreCase);
                                if (selectedAlbumId.Equals("all", StringComparison.InvariantCultureIgnoreCase))
                                    isSelected = x.Value.EndsWith("/all", StringComparison.InvariantCultureIgnoreCase);

                                if (isSelected)
                                    placeholder = x.Label;

                                return new DiscordSelectComponentOption(x.Label, x.Value, x.Description, isSelected);
                            })
                            .ToList();
                    }

                    DiscordSelectComponent albumSelector = new DiscordSelectComponent(previousAlbumSelector.CustomId, LimitStringSize(placeholder), albumOptions);
                    builder.AddComponents(albumSelector);
                }
            }

            return builder;
        }
    }
}
