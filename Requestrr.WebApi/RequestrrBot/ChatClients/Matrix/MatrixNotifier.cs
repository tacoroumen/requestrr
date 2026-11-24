using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Movies;
using Requestrr.WebApi.RequestrrBot.Notifications;
using Requestrr.WebApi.RequestrrBot.Notifications.Movies;
using Requestrr.WebApi.RequestrrBot.Notifications.TvShows;
using Requestrr.WebApi.RequestrrBot.TvShows;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    /// <summary>
    /// Tells Matrix users that their movie or tv show is available, either in configured rooms
    /// (mentioning requesters who are members) or in a direct-message room per user.
    /// </summary>
    public class MatrixNotifier : IMovieNotifier, ITvShowNotifier
    {
        private const string DirectRoomsFileName = "matrix_direct_rooms.json";
        private static readonly object FileLock = new object();

        private readonly MatrixApiClient _client;
        private readonly string _notificationMode;
        private readonly string[] _notificationRoomIds;
        private readonly Func<bool> _tvClientNotifiesPerSeason;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, string> _directRooms;

        public MatrixNotifier(MatrixApiClient client, string notificationMode, string[] notificationRoomIds, Func<bool> tvClientNotifiesPerSeason, ILogger logger)
        {
            _client = client;
            _notificationMode = notificationMode;
            _notificationRoomIds = notificationRoomIds;
            _tvClientNotifiesPerSeason = tvClientNotifiesPerSeason;
            _logger = logger;
            _directRooms = new ConcurrentDictionary<string, string>(LoadDirectRooms());
        }

        public Task<HashSet<string>> NotifyAsync(IReadOnlyCollection<string> userIds, Movie movie, CancellationToken token)
        {
            return NotifyUsersAsync(
                userIds,
                Language.Current.DiscordNotificationMovieChannel.ReplaceTokens(movie),
                Language.Current.DiscordNotificationMovieDM.ReplaceTokens(movie),
                movie.PosterPath,
                token);
        }

        public Task<HashSet<string>> NotifyAsync(IReadOnlyCollection<string> userIds, TvShow tvShow, int seasonNumber, CancellationToken token)
        {
            var perSeason = _tvClientNotifiesPerSeason();

            return NotifyUsersAsync(
                userIds,
                (perSeason ? Language.Current.DiscordNotificationTvChannelSeason : Language.Current.DiscordNotificationTvChannelFirstEpisode).ReplaceTokens(tvShow, seasonNumber),
                (perSeason ? Language.Current.DiscordNotificationTvDMSeason : Language.Current.DiscordNotificationTvDMFirstEpisode).ReplaceTokens(tvShow, seasonNumber),
                tvShow.Banner,
                token);
        }

        private async Task<HashSet<string>> NotifyUsersAsync(IReadOnlyCollection<string> userIds, string roomText, string directText, string imageUrl, CancellationToken token)
        {
            // Discord user IDs share the notification store; leave them for the Discord bot.
            var matrixUserIds = userIds.Where(MatrixIds.IsUserId).ToArray();

            if (!matrixUserIds.Any())
            {
                return new HashSet<string>();
            }

            return _notificationMode == NotificationMode.Channels
                ? await NotifyInRoomsAsync(matrixUserIds, roomText, imageUrl, token)
                : await NotifyDirectlyAsync(matrixUserIds, directText, imageUrl, token);
        }

        private async Task<HashSet<string>> NotifyInRoomsAsync(string[] userIds, string text, string imageUrl, CancellationToken token)
        {
            var notified = new HashSet<string>();

            foreach (var roomId in _notificationRoomIds)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    var members = await _client.GetJoinedMembersAsync(roomId, token);
                    var toMention = userIds.Where(x => members.ContainsKey(x) && !notified.Contains(x)).ToArray();

                    if (!toMention.Any()) continue;

                    var message = MatrixMessage.FromMarkdown(text).AppendLine();
                    message.IsNotice = false;
                    message.ImageUrl = imageUrl;

                    foreach (var userId in toMention)
                    {
                        message.AppendMarkdown(" ").AppendMention(userId, members[userId]);
                    }

                    await _client.SendMessageAsync(roomId, message, token);
                    notified.UnionWith(toMention);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not send a Matrix notification to room {roomId}: {ex.Message}");
                }
            }

            return notified;
        }

        private async Task<HashSet<string>> NotifyDirectlyAsync(string[] userIds, string text, string imageUrl, CancellationToken token)
        {
            var notified = new HashSet<string>();

            foreach (var userId in userIds)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    var roomId = await GetOrCreateDirectRoomAsync(userId, token);
                    var message = MatrixMessage.FromMarkdown(text);
                    message.IsNotice = false;
                    message.ImageUrl = imageUrl;
                    await _client.SendMessageAsync(roomId, message, token);
                    notified.Add(userId);
                    await Task.Delay(TimeSpan.FromMilliseconds(500), token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not send a Matrix direct notification to {userId}: {ex.Message}");
                }
            }

            return notified;
        }

        private async Task<string> GetOrCreateDirectRoomAsync(string userId, CancellationToken token)
        {
            if (_directRooms.TryGetValue(userId, out var roomId))
            {
                try
                {
                    var members = await _client.GetJoinedMembersAsync(roomId, token);

                    // The user may still have a pending invite, so only replace the room once everyone else has left it.
                    if (members.Count > 1 || members.ContainsKey(userId))
                    {
                        return roomId;
                    }

                    await _client.LeaveRoomAsync(roomId, token);
                }
                catch (MatrixApiException ex)
                {
                    _logger.LogWarning($"Direct room {roomId} for {userId} is no longer usable ({ex.ErrorCode}); creating a new one.");
                }
            }

            roomId = await _client.CreateDirectRoomAsync(userId, token);
            _directRooms[userId] = roomId;
            SaveDirectRooms(_directRooms);
            return roomId;
        }

        private static string DirectRoomsPath => Path.Combine(SettingsFile.SettingsFolder, DirectRoomsFileName);

        private static Dictionary<string, string> LoadDirectRooms()
        {
            lock (FileLock)
            {
                try
                {
                    if (File.Exists(DirectRoomsPath))
                    {
                        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(DirectRoomsPath)) ?? new Dictionary<string, string>();
                    }
                }
                catch
                {
                    // A corrupt cache only costs a new direct room.
                }

                return new Dictionary<string, string>();
            }
        }

        private static void SaveDirectRooms(IDictionary<string, string> rooms)
        {
            lock (FileLock)
            {
                File.WriteAllText(DirectRoomsPath, JsonConvert.SerializeObject(rooms, Formatting.Indented));
            }
        }
    }

    public static class MatrixIds
    {
        /// <summary>Matrix user IDs look like "@localpart:server".</summary>
        public static bool IsUserId(string value) => !string.IsNullOrEmpty(value) && value.StartsWith("@") && value.Contains(':');

        public static bool IsRoomId(string value) => !string.IsNullOrEmpty(value) && value.StartsWith("!") && value.Contains(':');

        public static bool IsRoomAlias(string value) => !string.IsNullOrEmpty(value) && value.StartsWith("#") && value.Contains(':');

        public static string ServerName(string id)
        {
            var index = id?.IndexOf(':') ?? -1;
            return index >= 0 ? id.Substring(index + 1) : string.Empty;
        }

        public static string Localpart(string userId)
        {
            if (!IsUserId(userId)) return userId;
            return userId.Substring(1, userId.IndexOf(':') - 1);
        }
    }
}
