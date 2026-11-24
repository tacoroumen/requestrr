using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Requestrr.WebApi.RequestrrBot.ChatClients.Discord;
using Requestrr.WebApi.RequestrrBot.DownloadClients;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Overseerr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Radarr;
using Requestrr.WebApi.RequestrrBot.DownloadClients.Sonarr;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Movies;
using Requestrr.WebApi.RequestrrBot.Music;
using Requestrr.WebApi.RequestrrBot.Notifications;
using Requestrr.WebApi.RequestrrBot.Notifications.Movies;
using Requestrr.WebApi.RequestrrBot.Notifications.TvShows;
using Requestrr.WebApi.RequestrrBot.TvShows;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    /// <summary>
    /// Runs Requestrr as a Matrix bot. It long-polls /sync, answers text commands in allowed rooms,
    /// and sends availability notifications. It runs when Matrix is turned on, alongside the Discord bot if that is on too.
    /// </summary>
    public class MatrixBot : IMatrixWorkflowProvider
    {
        private const int SyncTimeoutMs = 30000;
        private static readonly string SyncFilter = new JObject
        {
            ["presence"] = new JObject { ["not_types"] = new JArray("*") },
            ["account_data"] = new JObject { ["not_types"] = new JArray("*") },
            ["room"] = new JObject
            {
                ["timeline"] = new JObject { ["limit"] = 50, ["types"] = new JArray("m.room.message", "m.reaction") },
                ["state"] = new JObject { ["lazy_load_members"] = true, ["types"] = new JArray("m.room.member") },
                ["ephemeral"] = new JObject { ["not_types"] = new JArray("*") },
                ["account_data"] = new JObject { ["not_types"] = new JArray("*") },
            },
        }.ToString(Newtonsoft.Json.Formatting.None);

        private static readonly Dictionary<string, int> KeycapNumbers = new Dictionary<string, int>
        {
            ["1️⃣"] = 1, ["2️⃣"] = 2, ["3️⃣"] = 3, ["4️⃣"] = 4, ["5️⃣"] = 5,
            ["6️⃣"] = 6, ["7️⃣"] = 7, ["8️⃣"] = 8, ["9️⃣"] = 9, ["🔟"] = 10,
        };

        private static readonly HashSet<string> ConfirmWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "yes", "y", "ok", "confirm", "request", "✅", "👍" };
        private static readonly HashSet<string> CancelWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cancel", "no", "n", "stop", "❌" };
        private static readonly HashSet<string> ConfirmReactions = new HashSet<string> { "✅", "✔️", "☑️", "👍" };
        private static readonly HashSet<string> CancelReactions = new HashSet<string> { "❌", "✖️", "👎" };

        private readonly ILogger<MatrixBot> _logger;
        private readonly MatrixSettingsProvider _matrixSettingsProvider;
        private readonly DiscordSettingsProvider _downloadSettingsProvider;
        private readonly RadarrSettingsProvider _radarrSettingsProvider;
        private readonly SonarrSettingsProvider _sonarrSettingsProvider;
        private readonly OverseerrSettingsProvider _overseerrSettingsProvider;
        private readonly DownloadClients.Lidarr.LidarrSettingsProvider _lidarrSettingsProvider;
        private readonly RequestrrServices _services;
        private readonly MatrixConversationStore _conversations = new MatrixConversationStore();
        private readonly ConcurrentDictionary<string, string> _displayNames = new ConcurrentDictionary<string, string>();

        private MatrixApiClient _client;
        private CancellationTokenSource _sessionTokenSource;
        private Task _syncTask;
        private Task _pendingRequestsTask;
        private MatrixPendingRequests _pendingRequests;
        private string _runningConfigurationKey;
        private DateTime _nextStartAttemptUtc = DateTime.MinValue;
        private DateTime _sessionStartedUtc = DateTime.UtcNow;
        private MatrixSettings _settings = new MatrixSettings();
        private MatrixCommandCatalog _commands = new MatrixCommandCatalog();
        private HashSet<string> _monitoredRoomIds = new HashSet<string>();
        private HashSet<string> _notificationRoomIds = new HashSet<string>();
        private HashSet<string> _approvalRoomIds = new HashSet<string>();
        private string _botUserId;
        private string _botDisplayName;
        private MovieNotificationEngine _movieNotificationEngine;
        private TvShowNotificationEngine _tvShowNotificationEngine;
        private Notifications.Music.MusicNotificationEngine _musicNotificationEngine;

        /// <summary>
        /// Lists the members of the rooms the bot is in (including direct rooms), for the account picker on the Users page.
        /// </summary>
        public async Task<IReadOnlyList<ChatUser>> GetKnownUsersAsync(CancellationToken token)
        {
            var client = _client;
            var users = new Dictionary<string, ChatUser>(StringComparer.OrdinalIgnoreCase);

            if (client == null)
            {
                return Array.Empty<ChatUser>();
            }

            foreach (var roomId in (await client.GetJoinedRoomsAsync(token)).Take(100))
            {
                try
                {
                    foreach (var member in await client.GetJoinedMembersAsync(roomId, token))
                    {
                        if (string.Equals(member.Key, _botUserId, StringComparison.OrdinalIgnoreCase)) continue;

                        users.TryAdd(member.Key, new ChatUser { Id = member.Key, Name = member.Value, Platform = "Matrix", Detail = member.Key });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not list the members of Matrix room {roomId}: {ex.Message}");
                }
            }

            return users.Values.ToArray();
        }

        public MatrixBot(
            ILogger<MatrixBot> logger,
            MatrixSettingsProvider matrixSettingsProvider,
            DiscordSettingsProvider downloadSettingsProvider,
            RadarrSettingsProvider radarrSettingsProvider,
            SonarrSettingsProvider sonarrSettingsProvider,
            OverseerrSettingsProvider overseerrSettingsProvider,
            DownloadClients.Lidarr.LidarrSettingsProvider lidarrSettingsProvider,
            RequestrrServices services)
        {
            _logger = logger;
            _matrixSettingsProvider = matrixSettingsProvider;
            _downloadSettingsProvider = downloadSettingsProvider;
            _radarrSettingsProvider = radarrSettingsProvider;
            _sonarrSettingsProvider = sonarrSettingsProvider;
            _overseerrSettingsProvider = overseerrSettingsProvider;
            _lidarrSettingsProvider = lidarrSettingsProvider;
            _services = services;
        }

        public void Start()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await SuperviseAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while supervising the Matrix bot: " + ex.Message);
                    }

                    _conversations.PruneExpired();
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            });
        }

        private async Task SuperviseAsync()
        {
            var settings = _matrixSettingsProvider.Provide();
            var shouldRun = BotClientSelection.IsMatrixEnabled() && settings.IsConfigured;

            if (!shouldRun)
            {
                if (_client != null)
                {
                    _logger.LogWarning("Matrix bot disabled: stopping.");
                    await StopSessionAsync();
                }

                return;
            }

            var configurationKey = BuildConfigurationKey(settings);
            var sessionDied = _client != null && _syncTask != null && _syncTask.IsCompleted;

            if (_client != null && configurationKey == _runningConfigurationKey && !sessionDied)
            {
                return;
            }

            if (DateTime.UtcNow < _nextStartAttemptUtc)
            {
                return;
            }

            _logger.LogWarning(_client == null ? "Starting Matrix bot." : "Matrix bot configuration changed: restarting.");
            await StopSessionAsync();

            try
            {
                await StartSessionAsync(settings);
                _runningConfigurationKey = configurationKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Could not start the Matrix bot: {ex.Message}");
                await StopSessionAsync();
                _nextStartAttemptUtc = DateTime.UtcNow.AddMinutes(1);
            }
        }

        private string BuildConfigurationKey(MatrixSettings settings)
        {
            var download = _downloadSettingsProvider.Provide();

            return string.Join("|",
                settings.HomeserverUrl, settings.AccessToken, settings.CommandPrefix,
                string.Join(",", settings.MonitoredRooms), settings.NotificationMode, string.Join(",", settings.NotificationRooms), settings.AutomaticallyNotifyRequesters, string.Join(",", settings.ApprovalRooms),
                download.MovieDownloadClient, download.MovieDownloadClientConfigurationHash,
                download.TvShowDownloadClient, download.TvShowDownloadClientConfigurationHash,
                download.MusicDownloadClient, download.MusicDownloadClientConfigurationHash,
                Language.Current?.GetHashCode());
        }

        private async Task StartSessionAsync(MatrixSettings settings)
        {
            _settings = settings;
            _sessionStartedUtc = DateTime.UtcNow;
            _client = new MatrixApiClient(settings.HomeserverUrl, settings.AccessToken);
            _sessionTokenSource = new CancellationTokenSource();
            var token = _sessionTokenSource.Token;

            _botUserId = await _client.WhoAmIAsync(token);
            _botDisplayName = await _client.GetDisplayNameAsync(_botUserId, token);
            _logger.LogWarning($"Matrix bot signed in as {_botUserId}.");

            _monitoredRoomIds = await ResolveAndJoinRoomsAsync(settings.MonitoredRooms, token);
            var notificationRoomIds = settings.NotificationMode == NotificationMode.Channels
                ? await ResolveAndJoinRoomsAsync(settings.NotificationRooms, token)
                : new HashSet<string>();
            _notificationRoomIds = notificationRoomIds;
            _approvalRoomIds = await ResolveAndJoinRoomsAsync(settings.ApprovalRooms, token);

            _logger.LogWarning(_monitoredRoomIds.Any()
                ? $"Matrix bot listens in {string.Join(", ", _monitoredRoomIds)} and accepts invites to those rooms only."
                : $"Matrix bot listens in every room it joins and accepts invites from users on {MatrixIds.ServerName(_botUserId)}.");

            _commands = MatrixCommandCatalog.Build(
                _downloadSettingsProvider.Provide(),
                _radarrSettingsProvider.Provide(),
                _sonarrSettingsProvider.Provide(),
                _overseerrSettingsProvider.Provide(),
                _lidarrSettingsProvider.Provider(),
                GetMovieIssueClient() != null,
                GetTvShowIssueClient() != null);

            if (_commands.Commands.Any())
            {
                _logger.LogWarning($"Matrix bot commands: {string.Join(", ", _commands.Commands.Select(x => settings.CommandPrefix + x.Name))}.");
            }
            else
            {
                _logger.LogWarning("Matrix bot has no request commands: set up Radarr, Sonarr or Overseerr (with at least one category) in the web UI.");
            }

            StartNotificationEngines(settings, notificationRoomIds.ToArray());

            // The first sync only records where "now" is, so commands sent while the bot was offline are not replayed.
            // Synapse caches /sync responses for identical requests for a few minutes, so vary the timeout to get a fresh position.
            var initial = await _client.SyncAsync(null, Random.Shared.Next(1, 1000), SyncFilter, token);
            await HandleInvitesAsync(initial, token);
            var since = (string)initial["next_batch"];

            _syncTask = Task.Run(() => SyncLoopAsync(since, token));
            _pendingRequestsTask = Task.Run(() => PendingRequestsLoopAsync(token));
        }

        private async Task<HashSet<string>> ResolveAndJoinRoomsAsync(IEnumerable<string> rooms, CancellationToken token)
        {
            var resolved = new HashSet<string>();
            var joined = await _client.GetJoinedRoomsAsync(token);

            foreach (var room in rooms.Select(x => x?.Trim()).Where(x => !string.IsNullOrEmpty(x)))
            {
                string roomId;

                try
                {
                    roomId = MatrixIds.IsRoomAlias(room) ? await _client.ResolveRoomAliasAsync(room, token) : room;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Could not find Matrix room \"{room}\": {ex.Message}");
                    continue;
                }

                // Keep the room even when the join fails: an invite-only room can only be joined after an invite, which the bot then accepts.
                resolved.Add(roomId);

                if (joined.Contains(roomId))
                {
                    continue;
                }

                try
                {
                    await _client.JoinRoomAsync(roomId, token);
                    _logger.LogWarning($"Joined Matrix room {room}.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not join Matrix room {room} yet ({ex.Message}). Invite {_botUserId} to the room and the bot will join.");
                }
            }

            return resolved;
        }

        private void StartNotificationEngines(MatrixSettings settings, string[] notificationRoomIds)
        {
            if (settings.NotificationMode == NotificationMode.Disabled)
            {
                return;
            }

            var download = _downloadSettingsProvider.Provide();
            var notifier = new MatrixNotifier(
                _client,
                settings.NotificationMode,
                notificationRoomIds,
                () => _downloadSettingsProvider.Provide().TvShowDownloadClient == DownloadClient.Overseerr,
                _logger);

            if (download.MovieDownloadClient != DownloadClient.Disabled)
            {
                _movieNotificationEngine = _services.MovieWorkflowFactory.CreateMovieNotificationEngine(notifier, _logger);
                _movieNotificationEngine.Start();
            }

            if (download.TvShowDownloadClient != DownloadClient.Disabled)
            {
                _tvShowNotificationEngine = _services.TvShowWorkflowFactory.CreateTvShowNotificationEngine(notifier, _logger);
                _tvShowNotificationEngine.Start();
            }

            if (download.MusicDownloadClient != DownloadClient.Disabled)
            {
                _musicNotificationEngine = _services.MusicWorkflowFactory.CreateMusicNotificationEngine(notifier, _logger);
                _musicNotificationEngine.Start();
            }
        }

        private async Task StopSessionAsync()
        {
            _sessionTokenSource?.Cancel();

            try
            {
                if (_syncTask != null) await _syncTask;
                if (_pendingRequestsTask != null) await _pendingRequestsTask;
            }
            catch
            {
                // The loop ends with a cancellation exception.
            }

            try
            {
                if (_movieNotificationEngine != null) await _movieNotificationEngine.StopAsync();
                if (_tvShowNotificationEngine != null) await _tvShowNotificationEngine.StopAsync();
                if (_musicNotificationEngine != null) await _musicNotificationEngine.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while stopping Matrix notification engines: " + ex.Message);
            }

            _movieNotificationEngine = null;
            _tvShowNotificationEngine = null;
            _musicNotificationEngine = null;
            _syncTask = null;
            _client?.Dispose();
            _client = null;
            _sessionTokenSource?.Dispose();
            _sessionTokenSource = null;
            _runningConfigurationKey = null;
            _conversations.Reset();
        }

        private async Task SyncLoopAsync(string since, CancellationToken token)
        {
            var failures = 0;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var response = await _client.SyncAsync(since, SyncTimeoutMs, SyncFilter, token);
                    since = (string)response["next_batch"] ?? since;
                    failures = 0;

                    await HandleInvitesAsync(response, token);
                    HandleJoinedRooms(response);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (MatrixApiException ex) when (ex.ErrorCode == "M_UNKNOWN_TOKEN")
                {
                    _logger.LogError("The Matrix access token is no longer valid. Update it in the Requestrr settings.");
                    return;
                }
                catch (Exception ex)
                {
                    failures++;
                    var delay = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, failures)));
                    _logger.LogWarning($"Matrix sync failed ({ex.Message}); retrying in {delay.TotalSeconds}s.");
                    await Task.Delay(delay, token);
                }
            }
        }

        private async Task HandleInvitesAsync(JObject sync, CancellationToken token)
        {
            if (sync["rooms"]?["invite"] is not JObject invites) return;

            foreach (var invite in invites.Properties())
            {
                var roomId = invite.Name;
                var inviter = invite.Value["invite_state"]?["events"]?
                    .FirstOrDefault(e => (string)e["type"] == "m.room.member" && (string)e["state_key"] == _botUserId)?["sender"]?.ToString();

                if (!IsInviteAllowed(roomId, inviter))
                {
                    _logger.LogWarning(_monitoredRoomIds.Any()
                        ? $"Ignoring Matrix invite to {roomId} from {inviter ?? "unknown user"}: the room is not in the monitored rooms list."
                        : $"Ignoring Matrix invite to {roomId} from {inviter ?? "unknown user"}: only users on {MatrixIds.ServerName(_botUserId)} can invite the bot when no monitored rooms are set.");
                    continue;
                }

                var inviteState = invite.Value["invite_state"]?["events"] as JArray ?? new JArray();

                if (inviteState.Any(e => (string)e["type"] == "m.room.encryption"))
                {
                    await DeclineEncryptedRoomAsync(roomId, inviter, inviteState, token);
                    continue;
                }

                try
                {
                    await _client.JoinRoomAsync(roomId, token);
                    _logger.LogWarning($"Joined Matrix room {roomId} after an invite from {inviter}.");

                    // Homeservers do not always share the encryption state with the invite, so check again once joined.
                    if (await _client.IsRoomEncryptedAsync(roomId, token))
                    {
                        await DeclineEncryptedRoomAsync(roomId, inviter, inviteState, token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not join Matrix room {roomId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// With monitored rooms configured, the bot joins only those. Without them it accepts invites from users on its own homeserver,
        /// so strangers on other servers cannot pull the bot into their rooms.
        /// </summary>
        /// <summary>
        /// The bot does not support end-to-end encryption, so it cannot read or write in an encrypted room, so it leaves and tells the inviter in an unencrypted direct message.
        /// </summary>
        private async Task DeclineEncryptedRoomAsync(string roomId, string inviter, JArray inviteState, CancellationToken token)
        {
            var roomName = (string)inviteState.FirstOrDefault(e => (string)e["type"] == "m.room.name")?["content"]?["name"];

            _logger.LogWarning($"Leaving Matrix room {roomId}: it is encrypted, and the bot does not support encryption.");

            try { await _client.LeaveRoomAsync(roomId, token); } catch { }

            if (inviter == null) return;

            try
            {
                var directRoomId = await _client.CreateDirectRoomAsync(inviter, token);
                await _client.SendMessageAsync(directRoomId, MatrixMessage.FromMarkdown(
                    Language.Current.MatrixEncryptionRoomNotSupported.Replace("[Room]", string.IsNullOrWhiteSpace(roomName) ? roomId : roomName)), token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not tell {inviter} that {roomId} is encrypted: {ex.Message}");
            }
        }

        private bool IsInviteAllowed(string roomId, string inviter)
        {
            if (_notificationRoomIds.Contains(roomId) || _approvalRoomIds.Contains(roomId))
            {
                return true;
            }

            if (_monitoredRoomIds.Any())
            {
                return _monitoredRoomIds.Contains(roomId);
            }

            return inviter != null && MatrixIds.ServerName(inviter) == MatrixIds.ServerName(_botUserId);
        }

        private bool IsRoomAllowed(string roomId)
        {
            return !_monitoredRoomIds.Any() || _monitoredRoomIds.Contains(roomId) || _approvalRoomIds.Contains(roomId);
        }

        private void HandleJoinedRooms(JObject sync)
        {
            if (sync["rooms"]?["join"] is not JObject rooms) return;

            foreach (var room in rooms.Properties())
            {
                var roomId = room.Name;
                if (!IsRoomAllowed(roomId)) continue;

                var events = room.Value["timeline"]?["events"] as JArray;
                if (events == null) continue;

                foreach (var e in events.OfType<JObject>())
                {
                    var sender = (string)e["sender"];
                    if (string.IsNullOrEmpty(sender) || sender == _botUserId) continue;
                    if (IsFromBeforeSession(e)) continue;

                    var type = (string)e["type"];
                    var content = e["content"] as JObject;
                    if (content == null) continue;

                    if (type == "m.room.message" && (string)content["msgtype"] == "m.text")
                    {
                        var text = StripReplyFallback((string)content["body"] ?? string.Empty, content);
                        Dispatch(roomId, sender, context => HandleTextAsync(context, text));
                    }
                    else if (type == "m.reaction")
                    {
                        var relatesTo = content["m.relates_to"];
                        var targetEventId = (string)relatesTo?["event_id"];
                        var key = (string)relatesTo?["key"];

                        if (!string.IsNullOrEmpty(targetEventId) && !string.IsNullOrEmpty(key))
                        {
                            Dispatch(roomId, sender, context => HandleReactionAsync(context, targetEventId, key));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Skips events sent before this session started (with a short grace period), so a restart does not answer old commands twice.
        /// Uses the server-computed age, which does not depend on the two clocks agreeing.
        /// </summary>
        private bool IsFromBeforeSession(JObject e)
        {
            var ageMs = (long?)e["unsigned"]?["age"];
            if (ageMs == null) return false;

            var sessionUptimeMs = (DateTime.UtcNow - _sessionStartedUtc).TotalMilliseconds;
            return ageMs.Value > sessionUptimeMs + 10000;
        }

        private void Dispatch(string roomId, string userId, Func<MatrixCommandContext, Task> handler)
        {
            var client = _client;

            _ = Task.Run(() => _conversations.RunExclusiveAsync(roomId, userId, async () =>
            {
                var context = new MatrixCommandContext(client, _conversations, this, roomId, userId, await GetDisplayNameAsync(client, userId));

                try
                {
                    await handler(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error while handling a Matrix message from {userId} in {roomId}: {ex.Message}");
                    context.EndConversation();

                    try { await context.SendAsync(Language.Current.Error); } catch { }
                }
            }));
        }

        private async Task HandleTextAsync(MatrixCommandContext context, string text)
        {
            text = text.Trim();
            if (text.Length == 0) return;

            if (text.StartsWith(_settings.CommandPrefix, StringComparison.Ordinal))
            {
                await HandleCommandAsync(context, text.Substring(_settings.CommandPrefix.Length).Trim());
                return;
            }

            var prompt = _conversations.Get(context.RoomId, context.UserId);
            if (prompt == null) return;

            if (CancelWords.Contains(text))
            {
                context.EndConversation();
                await context.SendAsync(Language.Current.MatrixCancelled);
            }
            else if (prompt.IsSelection && int.TryParse(text.TrimEnd('.'), out var number))
            {
                await RunChoiceAsync(context, prompt, number);
            }
            else if (prompt.OnConfirm != null && ConfirmWords.Contains(text))
            {
                context.EndConversation();
                await WithTypingAsync(context, prompt.OnConfirm);
            }
            else if (prompt.OnText != null)
            {
                context.EndConversation();
                await WithTypingAsync(context, () => prompt.OnText(text));
            }
        }

        private async Task HandleReactionAsync(MatrixCommandContext context, string targetEventId, string key)
        {
            if (_approvalRoomIds.Contains(context.RoomId) && await TryHandleApprovalReactionAsync(context, targetEventId, key.Trim()))
            {
                return;
            }

            var prompt = _conversations.Get(context.RoomId, context.UserId);
            if (prompt == null || prompt.PromptEventId != targetEventId) return;

            key = key.Trim();

            if (CancelReactions.Contains(key))
            {
                context.EndConversation();
                await context.SendAsync(Language.Current.MatrixCancelled);
            }
            else if (prompt.IsSelection && KeycapNumbers.TryGetValue(key, out var number))
            {
                await RunChoiceAsync(context, prompt, number);
            }
            else if (prompt.OnConfirm != null && ConfirmReactions.Contains(key))
            {
                context.EndConversation();
                await WithTypingAsync(context, prompt.OnConfirm);
            }
        }

        private async Task RunChoiceAsync(MatrixCommandContext context, MatrixPrompt prompt, int number)
        {
            if (number < 1 || number > prompt.Choices.Count)
            {
                await context.SendAsync(Language.Current.MatrixSelectionPrompt.Replace(LanguageTokens.Count, prompt.Choices.Count.ToString()));
                return;
            }

            context.EndConversation();
            await WithTypingAsync(context, prompt.Choices[number - 1]);
        }

        private async Task HandleCommandAsync(MatrixCommandContext context, string commandLine)
        {
            var parts = commandLine.Split(new[] { ' ', '\t', '\n' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var name = parts[0].ToLowerInvariant();
            var argument = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            if (name == "cancel")
            {
                context.EndConversation();
                await context.SendAsync(Language.Current.MatrixCancelled);
                return;
            }

            if (name == Language.Current.DiscordCommandHelpRequestName?.ToLowerInvariant() || name == "help")
            {
                context.EndConversation();
                await context.SendAsync(await BuildHelpMessageAsync(context));
                return;
            }

            if (name == Language.Current.DiscordCommandPingRequestName?.ToLowerInvariant() || name == "ping")
            {
                await context.SendAsync(Language.Current.DiscordCommandPingResponse);
                return;
            }

            if (!_commands.TryGet(name, out var command))
            {
                await context.SendAsync(_commands.Commands.Any()
                    ? Language.Current.MatrixUnknownCommand
                        .Replace("[Command]", _settings.CommandPrefix + name)
                        .Replace("[CommandList]", string.Join(", ", _commands.Commands.Select(x => $"`{_settings.CommandPrefix}{x.Name}`")))
                        .Replace("[HelpCommand]", _settings.CommandPrefix + "help")
                    : Language.Current.MatrixNoRequestCommands);
                return;
            }

            context.EndConversation();

            if (string.IsNullOrWhiteSpace(argument))
            {
                await context.SendAsync(command.Usage(_settings.CommandPrefix));
                return;
            }

            if (!await CanRequestAsync(context, command))
            {
                return;
            }

            await WithTypingAsync(context, () => RunCommandAsync(context, command, argument));
        }

        /// <summary>
        /// Checks the request limits: exact power levels below 50 per category, while 50 and higher may always request.
        /// </summary>
        private async Task<bool> CanRequestAsync(MatrixCommandContext context, MatrixCommand command)
        {
            var settings = _matrixSettingsProvider.Provide();
            if (command.Kind == MatrixCommandKind.MovieIssue || command.Kind == MatrixCommandKind.TvShowIssue)
            {
                return true;
            }

            var isMovie = command.Kind == MatrixCommandKind.MovieByTitle || command.Kind == MatrixCommandKind.MovieByTmdbId;
            var isMusic = command.Kind == MatrixCommandKind.MusicArtist;
            var levels = isMusic ? settings.MusicRequestLevels : isMovie ? settings.MovieRequestLevels : settings.TvShowRequestLevels;

            if (levels == null || !levels.Any())
            {
                return true;
            }

            int powerLevel;

            try
            {
                powerLevel = await _client.GetPowerLevelAsync(context.RoomId, context.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not read the power level of {context.UserId} in {context.RoomId}: {ex.Message}");
                powerLevel = 0;
            }

            if (powerLevel >= MatrixSettings.AlwaysAllowedRequestLevel || levels.Contains(powerLevel))
            {
                return true;
            }

            await context.SendAsync(Language.Current.MatrixRequestNotAllowed.Replace("[Category]", isMusic ? "music" : isMovie ? "movies" : "tv shows"));
            return false;
        }

        private async Task RunCommandAsync(MatrixCommandContext context, MatrixCommand command, string argument)
        {
            switch (command.Kind)
            {
                case MatrixCommandKind.MovieByTitle:
                    await CreateMovieWorkflow(context, command.CategoryId).SearchMovieAsync(argument);
                    break;
                case MatrixCommandKind.MovieByTmdbId:
                    if (int.TryParse(argument, out var tmdbId))
                        await CreateMovieWorkflow(context, command.CategoryId).SearchMovieAsync(tmdbId);
                    else
                        await context.SendAsync(command.Usage(_settings.CommandPrefix));
                    break;
                case MatrixCommandKind.TvShowByTitle:
                    await CreateTvShowWorkflow(context, command.CategoryId).SearchTvShowAsync(argument);
                    break;
                case MatrixCommandKind.TvShowByTvdbId:
                    if (int.TryParse(argument, out var tvdbId))
                        await CreateTvShowWorkflow(context, command.CategoryId).SearchTvShowAsync(tvdbId);
                    else
                        await context.SendAsync(command.Usage(_settings.CommandPrefix));
                    break;
                case MatrixCommandKind.MusicArtist:
                    await CreateMusicWorkflow(context, command.CategoryId).SearchMusicForArtistAsync(argument);
                    break;
                case MatrixCommandKind.MovieIssue:
                    await ReportMovieIssueAsync(context, argument);
                    break;
                case MatrixCommandKind.TvShowIssue:
                    await ReportTvShowIssueAsync(context, argument);
                    break;
            }
        }

        /// <summary>
        /// The client that takes issue reports for movies, when the movie download client supports them and they are turned on.
        /// </summary>
        private object GetMovieIssueClient()
        {
            var download = _downloadSettingsProvider.Provide();

            if (download.MovieDownloadClient == DownloadClient.Overseerr && _overseerrSettingsProvider.Provide().UseMovieIssue) return _services.OverseerrClient;
            if (download.MovieDownloadClient == DownloadClient.Ombi && new DownloadClients.Ombi.OmbiSettingsProvider().Provide().UseMovieIssue) return _services.OmbiClient;
            return null;
        }

        private object GetTvShowIssueClient()
        {
            var download = _downloadSettingsProvider.Provide();

            if (download.TvShowDownloadClient == DownloadClient.Overseerr && _overseerrSettingsProvider.Provide().UseTVIssue) return _services.OverseerrClient;
            if (download.TvShowDownloadClient == DownloadClient.Ombi && new DownloadClients.Ombi.OmbiSettingsProvider().Provide().UseTVIssue) return _services.OmbiClient;
            return null;
        }

        private async Task ReportMovieIssueAsync(MatrixCommandContext context, string title)
        {
            if (GetMovieIssueClient() is not (IMovieIssueSearcher searcher and IMovieIssueRequester requester)) return;

            var request = new MovieRequest(new MovieUserRequester(context.UserId, context.DisplayName), MatrixCommandCatalog.IssueCategoryId);
            var movies = (await searcher.SearchMovieLibraryAsync(request, title.Replace(".", " "))).Take(10).ToArray();

            Func<Movie, Task> pick = movie => AskIssueTypeAsync(context, searcher.IssueTypes, movie.Title, Language.Current.MatrixIssueDescribeMovie,
                async (issueValue, description) =>
                {
                    var success = await requester.SubmitMovieIssueAsync(request, int.Parse(movie.TheMovieDbId), issueValue, description);
                    await context.SendAsync((success ? Language.Current.DiscordCommandMovieIssueSuccess : Language.Current.DiscordCommandMovieIssueFailed).Replace("[MovieTitle]", movie.Title));
                });

            await PickIssueTargetAsync(context, title, movies, x => string.IsNullOrWhiteSpace(x.ReleaseDate) || x.ReleaseDate.Length < 4 ? x.Title : $"{x.Title} ({x.ReleaseDate.Substring(0, 4)})", pick);
        }

        private async Task ReportTvShowIssueAsync(MatrixCommandContext context, string title)
        {
            if (GetTvShowIssueClient() is not (ITvShowIssueSearcher searcher and ITvShowIssueRequester requester)) return;

            var request = new TvShowRequest(new TvShowUserRequester(context.UserId, context.DisplayName), MatrixCommandCatalog.IssueCategoryId);
            var tvShows = (await searcher.SearchTvShowLibraryAsync(request, title.Replace(".", " "))).Take(10).ToArray();

            Func<SearchedTvShow, Task> pick = tvShow => AskIssueTypeAsync(context, searcher.IssueTypes, tvShow.Title, Language.Current.MatrixIssueDescribeTv,
                async (issueValue, description) =>
                {
                    var success = await requester.SubmitTvShowIssueAsync(request, tvShow.TheTvDbId, issueValue, description);
                    await context.SendAsync((success ? Language.Current.DiscordCommandTvIssueSuccess : Language.Current.DiscordCommandTvIssueFailed).Replace("[TvShowTitle]", tvShow.Title));
                });

            await PickIssueTargetAsync(context, title, tvShows, x => string.IsNullOrWhiteSpace(x.FirstAired) || x.FirstAired.Length < 4 ? x.Title : $"{x.Title} ({x.FirstAired.Substring(0, 4)})", pick);
        }

        private static async Task PickIssueTargetAsync<T>(MatrixCommandContext context, string query, IReadOnlyList<T> found, Func<T, string> label, Func<T, Task> pick)
        {
            if (found.Count == 0)
            {
                await context.SendAsync(Language.Current.MatrixIssueNotFound.Replace("[Title]", query));
            }
            else if (found.Count == 1)
            {
                await pick(found[0]);
            }
            else
            {
                await context.AskToChooseAsync(
                    MatrixMessage.FromMarkdown(Language.Current.MatrixIssueSelect).AppendLine(),
                    found.Select(label).ToArray(),
                    found.Select(x => (Func<Task>)(() => pick(x))).ToArray());
            }
        }

        private static async Task AskIssueTypeAsync(MatrixCommandContext context, IReadOnlyDictionary<string, int> issueTypes, string title, string describeTemplate, Func<string, string, Task> submit)
        {
            var types = issueTypes.ToArray();

            await context.AskToChooseAsync(
                MatrixMessage.FromMarkdown(Language.Current.MatrixIssueType.Replace("[Title]", title)).AppendLine(),
                types.Select(x => x.Key).ToArray(),
                types.Select(type => (Func<Task>)(() => context.AskForTextAsync(
                    MatrixMessage.FromMarkdown(describeTemplate.Replace("[Issue]", type.Key).Replace("[Title]", title)),
                    description => submit(type.Value.ToString(), description)))).ToArray());
        }

        private async Task<MatrixMessage> BuildHelpMessageAsync(MatrixCommandContext context)
        {
            var prefix = _settings.CommandPrefix;
            var language = Language.Current;
            var sections = new List<string>();

            void AddSection(string heading, IEnumerable<string> lines)
            {
                var list = lines.ToList();
                if (list.Any())
                {
                    sections.Add($"**{heading}**\n" + string.Join("\n", list));
                }
            }

            string Describe(MatrixCommand command, string description, int sameKindCount)
            {
                var line = $"• {command.Usage(prefix)}: {description}";
                return sameKindCount > 1 ? line + " " + language.MatrixHelpCategoryNote.Replace("[Category]", command.Name) : line;
            }

            IEnumerable<string> Lines(MatrixCommandKind kind, string description)
            {
                var commands = _commands.Commands.Where(x => x.Kind == kind).ToList();
                return commands.Select(x => Describe(x, description, commands.Count));
            }

            AddSection(language.MatrixHelpSectionMovies, Lines(MatrixCommandKind.MovieByTitle, language.MatrixHelpMovieByTitle)
                .Concat(Lines(MatrixCommandKind.MovieByTmdbId, language.MatrixHelpMovieByTmdbId)));
            AddSection(language.MatrixHelpSectionTvShows, Lines(MatrixCommandKind.TvShowByTitle, language.MatrixHelpTvShowByTitle)
                .Concat(Lines(MatrixCommandKind.TvShowByTvdbId, language.MatrixHelpTvShowByTvdbId)));
            AddSection(language.MatrixHelpSectionMusic, Lines(MatrixCommandKind.MusicArtist, language.MatrixHelpMusicArtist));
            AddSection(language.MatrixHelpSectionIssues, Lines(MatrixCommandKind.MovieIssue, language.MatrixHelpMovieIssue)
                .Concat(Lines(MatrixCommandKind.TvShowIssue, language.MatrixHelpTvShowIssue)));
            AddSection(language.MatrixHelpSectionOther, new[]
            {
                $"• `{prefix}help`: {language.MatrixHelpHelp}",
                $"• `{prefix}cancel`: {language.MatrixHelpCancel}",
                $"• `{prefix}ping`: {language.MatrixHelpPing}",
            });

            if (!_commands.Commands.Any())
            {
                sections.Insert(0, language.MatrixNoRequestCommands);
            }

            var examples = new List<string>();
            void AddExample(MatrixCommandKind kind, string example)
            {
                var command = _commands.Commands.FirstOrDefault(x => x.Kind == kind);
                if (command != null) examples.Add($"`{prefix}{command.Name} {example}`");
            }

            AddExample(MatrixCommandKind.MovieByTitle, "Dune");
            AddExample(MatrixCommandKind.TvShowByTitle, "The Bear");
            AddExample(MatrixCommandKind.MusicArtist, "Daft Punk");

            var text = language.MatrixHelpMessage
                .Replace(LanguageTokens.AuthorUsername, context.DisplayName)
                .Replace(LanguageTokens.BotUsername, _botDisplayName ?? _botUserId)
                .Replace(LanguageTokens.CommandPrefix, prefix)
                .Replace(LanguageTokens.MovieCommandTitle, prefix + _commands.FirstMovieCommandName())
                .Replace(LanguageTokens.FullCommandList, string.Join("\n\n", sections))
                .Replace("[Access]", await BuildAccessLineAsync(context))
                .Replace("[Examples]", examples.Any() ? language.MatrixHelpExamples.Replace("[Examples]", string.Join(", ", examples)) : string.Empty);

            return MatrixMessage.FromMarkdown(Regex.Replace(text, "\n{3,}", "\n\n").Trim());
        }

        /// <summary>
        /// Tells the caller which request types the room's power-level limits allow for them.
        /// </summary>
        private async Task<string> BuildAccessLineAsync(MatrixCommandContext context)
        {
            var settings = _matrixSettingsProvider.Provide();
            var kinds = new List<(string Name, int[] Levels)>();

            if (_commands.Commands.Any(x => x.Kind == MatrixCommandKind.MovieByTitle || x.Kind == MatrixCommandKind.MovieByTmdbId))
                kinds.Add((Language.Current.MatrixHelpAccessMovies, settings.MovieRequestLevels));
            if (_commands.Commands.Any(x => x.Kind == MatrixCommandKind.TvShowByTitle || x.Kind == MatrixCommandKind.TvShowByTvdbId))
                kinds.Add((Language.Current.MatrixHelpAccessTvShows, settings.TvShowRequestLevels));
            if (_commands.Commands.Any(x => x.Kind == MatrixCommandKind.MusicArtist))
                kinds.Add((Language.Current.MatrixHelpAccessMusic, settings.MusicRequestLevels));

            if (!kinds.Any())
            {
                return string.Empty;
            }

            var powerLevel = 0;
            if (kinds.Any(x => x.Levels != null && x.Levels.Any()))
            {
                try
                {
                    powerLevel = await _client.GetPowerLevelAsync(context.RoomId, context.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not read the power level of {context.UserId} in {context.RoomId}: {ex.Message}");
                }
            }

            bool Allowed(int[] levels) => levels == null || !levels.Any() || powerLevel >= MatrixSettings.AlwaysAllowedRequestLevel || levels.Contains(powerLevel);

            var allowed = kinds.Where(x => Allowed(x.Levels)).Select(x => x.Name).ToList();
            var denied = kinds.Where(x => !Allowed(x.Levels)).Select(x => x.Name).ToList();

            if (!denied.Any())
            {
                return Language.Current.MatrixHelpAccessAll.Replace("[Allowed]", JoinWords(allowed));
            }

            if (!allowed.Any())
            {
                return Language.Current.MatrixHelpAccessNone.Replace("[Denied]", JoinWords(denied));
            }

            return Language.Current.MatrixHelpAccessSome
                .Replace("[Allowed]", JoinWords(allowed))
                .Replace("[Denied]", JoinWords(denied));
        }

        private static string JoinWords(IReadOnlyList<string> words)
        {
            return words.Count <= 1
                ? string.Join(string.Empty, words)
                : string.Join(", ", words.Take(words.Count - 1)) + " " + Language.Current.MatrixHelpAnd + " " + words.Last();
        }

        private async Task WithTypingAsync(MatrixCommandContext context, Func<Task> work)
        {
            try { await context.Client.SendTypingAsync(context.RoomId, _botUserId, true); } catch { }

            try
            {
                await work();
            }
            finally
            {
                try { await context.Client.SendTypingAsync(context.RoomId, _botUserId, false); } catch { }
            }
        }

        private async Task<string> GetDisplayNameAsync(MatrixApiClient client, string userId)
        {
            if (_displayNames.TryGetValue(userId, out var cached)) return cached;

            var displayName = await client.GetDisplayNameAsync(userId);
            _displayNames[userId] = displayName;
            return displayName;
        }

        /// <summary>
        /// Removes the quoted text that Matrix clients put at the top of a reply's plain-text body.
        /// </summary>
        private static string StripReplyFallback(string body, JObject content)
        {
            if (content["m.relates_to"]?["m.in_reply_to"] == null) return body;

            var lines = body.Replace("\r\n", "\n").Split('\n').SkipWhile(line => line.StartsWith(">")).SkipWhile(string.IsNullOrWhiteSpace);
            return string.Join("\n", lines);
        }

        public MovieRequestingWorkflow CreateMovieWorkflow(MatrixCommandContext context, int categoryId)
        {
            return _services.MovieWorkflowFactory.CreateRequestingWorkflow(
                new MovieUserRequester(context.UserId, context.DisplayName),
                categoryId,
                new MatrixMovieUserInterface(context),
                _settings.NotificationMode,
                _settings.AutomaticallyNotifyRequesters);
        }

        public IMovieNotificationWorkflow CreateMovieNotificationWorkflow(MatrixCommandContext context)
        {
            return _services.MovieWorkflowFactory.CreateNotificationWorkflow(new MatrixMovieUserInterface(context), _settings.NotificationMode, _settings.AutomaticallyNotifyRequesters);
        }

        public MusicRequestingWorkflow CreateMusicWorkflow(MatrixCommandContext context, int categoryId)
        {
            return _services.MusicWorkflowFactory.CreateRequestingWorkflow(
                new MusicUserRequester(context.UserId, context.DisplayName),
                categoryId,
                new MatrixMusicUserInterface(context),
                _settings.NotificationMode,
                _settings.AutomaticallyNotifyRequesters);
        }

        public IMusicNotificationWorkflow CreateMusicNotificationWorkflow(MatrixCommandContext context)
        {
            return _services.MusicWorkflowFactory.CreateNotificationWorkflow(new MatrixMusicUserInterface(context), _settings.NotificationMode, _settings.AutomaticallyNotifyRequesters);
        }

        public IMovieSearcher GetMovieSearcher()
        {
            return _services.MovieWorkflowFactory.GetMovieSearcher();
        }

        public async Task TrackPendingRequestAsync(MatrixCommandContext context, int requestId, string eventId, string title, MatrixMessage details)
        {
            PendingRequests.Track(requestId, context.RoomId, eventId, title);

            var download = _downloadSettingsProvider.Provide();
            if (download.MovieDownloadClient != DownloadClient.Overseerr && download.TvShowDownloadClient != DownloadClient.Overseerr)
            {
                return;
            }

            foreach (var roomId in _approvalRoomIds)
            {
                try
                {
                    var prompt = Language.Current.MatrixApprovalPrompt.Split("[Requester]");
                    var adminMessage = CloneDetails(details);
                    adminMessage.AppendMarkdown(prompt[0]).AppendMention(context.UserId, context.DisplayName).AppendMarkdown(prompt.Length > 1 ? prompt[1] : string.Empty);
                    adminMessage.IsNotice = false;

                    var adminEventId = await _client.SendMessageAsync(roomId, adminMessage);
                    PendingRequests.Track(requestId, roomId, adminEventId, title, isApprovalMessage: true, requesterName: context.DisplayName ?? context.UserId);

                    await _client.SendReactionAsync(roomId, adminEventId, MatrixCommandContext.ConfirmReaction);
                    await _client.SendReactionAsync(roomId, adminEventId, MatrixCommandContext.CancelReaction);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not post request {requestId} to approval room {roomId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Posts a request that went through or was denied right away to the approval rooms, when that option is on.
        /// Matches the Discord "send all requests to admin channels" option: information only, no reactions.
        /// </summary>
        public async Task PostRequestSummaryAsync(MatrixCommandContext context, MatrixMessage details, string status)
        {
            if (!_matrixSettingsProvider.Provide().ApprovalRoomsAllRequests)
            {
                return;
            }

            foreach (var roomId in _approvalRoomIds)
            {
                try
                {
                    var summary = Language.Current.DiscordCommandRequestAdminSummary.Replace(LanguageTokens.RequestStatus, status).Split(LanguageTokens.AuthorUsername);
                    var message = CloneDetails(details);
                    message.AppendMarkdown(summary[0]).AppendMention(context.UserId, context.DisplayName).AppendMarkdown(summary.Length > 1 ? summary[1] : string.Empty);
                    await _client.SendMessageAsync(roomId, message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not post a request summary to approval room {roomId}: {ex.Message}");
                }
            }
        }

        private static MatrixMessage CloneDetails(MatrixMessage details)
        {
            return MatrixMessage.FromParts(details);
        }

        /// <summary>
        /// A ✅ or ❌ on a request in an approval room approves or denies it in Overseerr, if the person is a moderator of that room.
        /// </summary>
        /// <summary>
        /// Seerr decides for linked users (Admin or Manage Requests); unlinked users need the configured power level in the room.
        /// </summary>
        private async Task<bool> IsRequestApproverAsync(MatrixCommandContext context)
        {
            try
            {
                var seerrDecision = await _services.OverseerrClient.CanManageRequestsAsync(context.UserId);
                if (seerrDecision.HasValue)
                {
                    return seerrDecision.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not read the Seerr permissions of {context.UserId}, falling back to the room power level: {ex.Message}");
            }

            return await _client.GetPowerLevelAsync(context.RoomId, context.UserId) >= _matrixSettingsProvider.Provide().ApprovalPowerLevel;
        }

        private async Task<bool> TryHandleApprovalReactionAsync(MatrixCommandContext context, string targetEventId, string key)
        {
            var entry = PendingRequests.FindApprovalMessage(context.RoomId, targetEventId);
            if (entry == null) return false;

            var approve = ConfirmReactions.Contains(key);
            var deny = CancelReactions.Contains(key);
            if (!approve && !deny) return true;

            if (!await IsRequestApproverAsync(context))
            {
                await context.SendAsync(Language.Current.MatrixApprovalNotAllowed);
                return true;
            }

            if (approve)
            {
                await _services.OverseerrClient.ApproveRequestAsync(entry.RequestId);
            }
            else
            {
                await _services.OverseerrClient.DeclineRequestAsync(entry.RequestId);
            }

            _logger.LogWarning($"Matrix user {context.UserId} {(approve ? "approved" : "denied")} Overseerr request {entry.RequestId}.");
            await PendingRequests.CompleteAsync(_client, entry.RequestId, approve, context.DisplayName ?? context.UserId, CancellationToken.None);
            return true;
        }

        private MatrixPendingRequests PendingRequests => _pendingRequests ??= new MatrixPendingRequests(_logger);

        /// <summary>
        /// Once a minute, edits "pending approval" messages whose Overseerr request was approved or declined.
        /// </summary>
        private async Task PendingRequestsLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), token);

                    var download = _downloadSettingsProvider.Provide();
                    if (download.MovieDownloadClient == DownloadClient.Overseerr || download.TvShowDownloadClient == DownloadClient.Overseerr)
                    {
                        await PendingRequests.CheckAsync(_client, _services.OverseerrClient, token);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error while checking pending Matrix requests: {ex.Message}");
                }
            }
        }

        public TvShowRequestingWorkflow CreateTvShowWorkflow(MatrixCommandContext context, int categoryId)
        {
            return _services.TvShowWorkflowFactory.CreateRequestingWorkflow(
                new TvShowUserRequester(context.UserId, context.DisplayName),
                categoryId,
                new MatrixTvShowUserInterface(context),
                _settings.NotificationMode,
                _settings.AutomaticallyNotifyRequesters);
        }

        public ITvShowNotificationWorkflow CreateTvShowNotificationWorkflow(MatrixCommandContext context)
        {
            return _services.TvShowWorkflowFactory.CreateNotificationWorkflow(new MatrixTvShowUserInterface(context), _settings.NotificationMode, _settings.AutomaticallyNotifyRequesters);
        }
    }
}
