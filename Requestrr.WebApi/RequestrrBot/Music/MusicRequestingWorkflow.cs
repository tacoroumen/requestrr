using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public class MusicRequestingWorkflow
    {
        private readonly int _categoryId;
        private readonly MusicUserRequester _user;
        private readonly IMusicSearcher _musicSearcher;
        private readonly IMusicRequester _requester;
        private readonly IMusicUserInterface _userInterface;
        private readonly IMusicNotificationWorkflow _notificationWorkflow;


        public MusicRequestingWorkflow(
            MusicUserRequester user,
            int categoryId,
            IMusicSearcher searcher,
            IMusicRequester requester,
            IMusicUserInterface userInterface,
            IMusicNotificationWorkflow notificationWorkflow
        )
        {
            _categoryId = categoryId;
            _user = user;
            _musicSearcher = searcher;
            _requester = requester;
            _userInterface = userInterface;
            _notificationWorkflow = notificationWorkflow;
        }


        public async Task SearchMusicForArtistAsync(string artistName)
        {
            IReadOnlyList<MusicArtist> musicList = await SearchMusicForArtistListAsync(artistName);

            if (musicList.Any())
            {
                if (musicList.Count > 1)
                {
                    await _userInterface.ShowMusicArtistSelection(new MusicRequest(_user, _categoryId), musicList);
                }
                else if (musicList.Count == 1)
                {
                    MusicArtist music = musicList.Single();
                    await HandleMusicSelectionAsync(music);
                }
            }
        }


        public async Task<IReadOnlyList<MusicArtist>> SearchMusicForArtistListAsync(string artistName)
        {
            IReadOnlyList<MusicArtist> music = Array.Empty<MusicArtist>();

            artistName = artistName.Replace(".", " ");
            music = await _musicSearcher.SearchMusicForArtistAsync(new MusicRequest(_user, _categoryId), artistName);

            if (!music.Any())
                await _userInterface.WarnNoMusicArtistFoundAsync(artistName);

            return music;
        }


        public async Task HandleMusicArtistSelectionAsync(string musicArtistId)
        {
            await HandleMusicSelectionAsync(await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), musicArtistId));
        }

        public async Task HandleMusicAlbumSelectionAsync(string musicArtistId, string albumId)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), musicArtistId);

            if (musicArtist == null)
            {
                await _userInterface.WarnNoMusicArtistFoundAsync(musicArtistId);
                return;
            }

            if (string.Equals(albumId, "all", StringComparison.InvariantCultureIgnoreCase))
            {
                await HandleMusicArtistRequestSelectionAsync(musicArtist);
                return;
            }

            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchMusicAlbumsForArtistAsync(new MusicRequest(_user, _categoryId), musicArtist);
            MusicAlbum selectedAlbum = albums.FirstOrDefault(x => x.AlbumId == albumId);

            if (selectedAlbum == null)
            {
                await HandleMusicArtistRequestSelectionAsync(musicArtist);
                return;
            }

            if (CanAlbumBeRequested(selectedAlbum))
            {
                await _userInterface.DisplayMusicAlbumDetailsAsync(new MusicRequest(_user, _categoryId), musicArtist, selectedAlbum);
            }
            else if (selectedAlbum.Available)
            {
                await _userInterface.WarnMusicAlbumAlreadyAvailableAsync(musicArtist, selectedAlbum);
            }
            else
            {
                await _userInterface.WarnMusicAlbumAlreadyRequestedAsync(musicArtist, selectedAlbum);
            }
        }


        private async Task HandleMusicSelectionAsync(MusicArtist musicArtist)
        {
            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchMusicAlbumsForArtistAsync(new MusicRequest(_user, _categoryId), musicArtist);
            if (albums.Any())
            {
                await _userInterface.ShowMusicAlbumSelection(new MusicRequest(_user, _categoryId), musicArtist, albums, 0);
                return;
            }

            await HandleMusicArtistRequestSelectionAsync(musicArtist);
        }

        private async Task HandleMusicArtistRequestSelectionAsync(MusicArtist musicArtist)
        {
            if (CanBeRequested(musicArtist))
            {
                await _userInterface.DisplayMusicArtistDetailsAsync(new MusicRequest(_user, _categoryId), musicArtist);
            }
            else
            {
                if (musicArtist.Available)
                {
                    await _userInterface.WarnMusicArtistAlreadyAvailableAsync(musicArtist);
                }
                else
                {
                    await _notificationWorkflow.NotifyForExistingRequestAsync(_user.UserId, musicArtist);
                }
            }
        }



        /// <summary>
        /// Handles the request for an artist
        /// </summary>
        /// <param name="artistId"></param>
        /// <returns></returns>
        public async Task RequestMusicArtistAsync(string artistId)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), artistId);
            MusicRequestResult result = await _requester.RequestMusicAsync(new MusicRequest(_user, _categoryId), musicArtist);

            if (result.WasDenied)
            {
                await _userInterface.DisplayArtistRequestDeniedAsync(musicArtist);
            }
            else
            {
                await _userInterface.DisplayArtistRequestSuccessAsync(musicArtist);
                await _notificationWorkflow.NotifyForNewRequestAsync(_user.UserId, musicArtist);
            }
        }

        public async Task RequestMusicAlbumAsync(string artistId, string albumId)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), artistId);
            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchMusicAlbumsForArtistAsync(new MusicRequest(_user, _categoryId), musicArtist);
            MusicAlbum selectedAlbum = albums.FirstOrDefault(x => x.AlbumId == albumId);

            if (selectedAlbum == null)
            {
                return;
            }

            MusicRequestResult result = await _requester.RequestMusicAlbumAsync(new MusicRequest(_user, _categoryId), musicArtist, selectedAlbum);

            if (result.WasDenied)
            {
                await _userInterface.DisplayMusicAlbumRequestDeniedAsync(musicArtist, selectedAlbum);
            }
            else
            {
                await _userInterface.DisplayMusicAlbumRequestSuccessAsync(musicArtist, selectedAlbum);
            }
        }

        public async Task ShowMusicAlbumPageAsync(string artistId, int page)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), artistId);
            if (musicArtist == null)
            {
                await _userInterface.WarnNoMusicArtistFoundAsync(artistId);
                return;
            }

            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchMusicAlbumsForArtistAsync(new MusicRequest(_user, _categoryId), musicArtist);
            if (!albums.Any())
            {
                await HandleMusicArtistRequestSelectionAsync(musicArtist);
                return;
            }

            await _userInterface.ShowMusicAlbumSelection(new MusicRequest(_user, _categoryId), musicArtist, albums, page);
        }



        private static bool CanBeRequested(MusicArtist music)
        {
            return !music.Available && !music.Requested;
        }

        private static bool CanAlbumBeRequested(MusicAlbum album)
        {
            return !album.Available && !album.Requested;
        }
    }
}
