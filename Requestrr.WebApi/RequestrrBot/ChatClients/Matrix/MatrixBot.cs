using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private readonly RequestrrServices _services;
        private readonly MatrixConversationStore _conversations = new MatrixConversationStore();
        private readonly ConcurrentDictionary<string, string> _displayNames = new ConcurrentDictionary<string, string>();

        private MatrixApiClient _client;
        private CancellationTokenSource _sessionTokenSource;
        private Task _syncTask;
        private string _runningConfigurationKey;
        private DateTime _nextStartAttemptUtc = DateTime.MinValue;
        private DateTime _sessionStartedUtc = DateTime.UtcNow;
        private MatrixSettings _settings = new MatrixSettings();
        private MatrixCommandCatalog _commands = new MatrixCommandCatalog();
        private HashSet<string> _monitoredRoomIds = new HashSet<string>();
        private string _botUserId;
        private string _botDisplayName;
        private MovieNotificationEngine _movieNotificationEngine;
        private TvShowNotificationEngine _tvShowNotificationEngine;

        public MatrixBot(
            ILogger<MatrixBot> logger,
            MatrixSettingsProvider matrixSettingsProvider,
            DiscordSettingsProvider downloadSettingsProvider,
            RadarrSettingsProvider radarrSettingsProvider,
            SonarrSettingsProvider sonarrSettingsProvider,
            OverseerrSettingsProvider overseerrSettingsProvider,
            RequestrrServices services)
        {
            _logger = logger;
            _matrixSettingsProvider = matrixSettingsProvider;
            _downloadSettingsProvider = downloadSettingsProvider;
            _radarrSettingsProvider = radarrSettingsProvider;
            _sonarrSettingsProvider = sonarrSettingsProvider;
            _overseerrSettingsProvider = overseerrSettingsProvider;
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
                string.Join(",", settings.MonitoredRooms), settings.NotificationMode, string.Join(",", settings.NotificationRooms), settings.AutomaticallyNotifyRequesters,
                download.MovieDownloadClient, download.MovieDownloadClientConfigurationHash,
                download.TvShowDownloadClient, download.TvShowDownloadClientConfigurationHash,
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
            _logger.LogInformation($"Matrix bot signed in as {_botUserId}.");

            _monitoredRoomIds = await ResolveAndJoinRoomsAsync(settings.MonitoredRooms, token);
            var notificationRoomIds = settings.NotificationMode == NotificationMode.Channels
                ? await ResolveAndJoinRoomsAsync(settings.NotificationRooms, token)
                : new HashSet<string>();

            _commands = MatrixCommandCatalog.Build(
                _downloadSettingsProvider.Provide(),
                _radarrSettingsProvider.Provide(),
                _sonarrSettingsProvider.Provide(),
                _overseerrSettingsProvider.Provide());

            StartNotificationEngines(settings, notificationRoomIds.ToArray());

            // The first sync only records where "now" is, so commands sent while the bot was offline are not replayed.
            // Synapse caches /sync responses for identical requests for a few minutes, so vary the timeout to get a fresh position.
            var initial = await _client.SyncAsync(null, Random.Shared.Next(1, 1000), SyncFilter, token);
            await HandleInvitesAsync(initial, token);
            var since = (string)initial["next_batch"];

            _syncTask = Task.Run(() => SyncLoopAsync(since, token));
        }

        private async Task<HashSet<string>> ResolveAndJoinRoomsAsync(IEnumerable<string> rooms, CancellationToken token)
        {
            var resolved = new HashSet<string>();
            var joined = await _client.GetJoinedRoomsAsync(token);

            foreach (var room in rooms)
            {
                try
                {
                    var roomId = MatrixIds.IsRoomAlias(room) ? await _client.ResolveRoomAliasAsync(room, token) : room;

                    if (!joined.Contains(roomId))
                    {
                        roomId = await _client.JoinRoomAsync(roomId, token);
                    }

                    resolved.Add(roomId);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Could not resolve or join Matrix room \"{room}\": {ex.Message}");
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
        }

        private async Task StopSessionAsync()
        {
            _sessionTokenSource?.Cancel();

            try
            {
                if (_syncTask != null) await _syncTask;
            }
            catch
            {
                // The loop ends with a cancellation exception.
            }

            try
            {
                if (_movieNotificationEngine != null) await _movieNotificationEngine.StopAsync();
                if (_tvShowNotificationEngine != null) await _tvShowNotificationEngine.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while stopping Matrix notification engines: " + ex.Message);
            }

            _movieNotificationEngine = null;
            _tvShowNotificationEngine = null;
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
                    _logger.LogInformation($"Ignoring Matrix invite to {roomId} from {inviter ?? "unknown user"}.");
                    continue;
                }

                try
                {
                    await _client.JoinRoomAsync(roomId, token);
                    _logger.LogInformation($"Joined Matrix room {roomId} after an invite from {inviter}.");
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
        private bool IsInviteAllowed(string roomId, string inviter)
        {
            if (_monitoredRoomIds.Any())
            {
                return _monitoredRoomIds.Contains(roomId);
            }

            return inviter != null && MatrixIds.ServerName(inviter) == MatrixIds.ServerName(_botUserId);
        }

        private bool IsRoomAllowed(string roomId)
        {
            return !_monitoredRoomIds.Any() || _monitoredRoomIds.Contains(roomId);
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
        }

        private async Task HandleReactionAsync(MatrixCommandContext context, string targetEventId, string key)
        {
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
                await context.SendAsync(BuildHelpMessage(context));
                return;
            }

            if (name == Language.Current.DiscordCommandPingRequestName?.ToLowerInvariant() || name == "ping")
            {
                await context.SendAsync(Language.Current.DiscordCommandPingResponse);
                return;
            }

            if (!_commands.TryGet(name, out var command))
            {
                return;
            }

            context.EndConversation();

            if (string.IsNullOrWhiteSpace(argument))
            {
                await context.SendAsync(command.Usage(_settings.CommandPrefix));
                return;
            }

            await WithTypingAsync(context, () => RunCommandAsync(context, command, argument));
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
            }
        }

        private MatrixMessage BuildHelpMessage(MatrixCommandContext context)
        {
            var prefix = _settings.CommandPrefix;
            var commandList = _commands.Commands.Select(x => "• " + x.Usage(prefix))
                .Append($"• `{prefix}help`")
                .Append($"• `{prefix}cancel`");

            var text = Language.Current.MatrixHelpMessage
                .Replace(LanguageTokens.AuthorUsername, context.DisplayName)
                .Replace(LanguageTokens.BotUsername, _botDisplayName ?? _botUserId)
                .Replace(LanguageTokens.CommandPrefix, prefix)
                .Replace(LanguageTokens.MovieCommandTitle, prefix + _commands.FirstMovieCommandName())
                .Replace(LanguageTokens.FullCommandList, string.Join("\n", commandList));

            return MatrixMessage.FromMarkdown(text);
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

        public IMovieSearcher GetMovieSearcher()
        {
            return _services.MovieWorkflowFactory.GetMovieSearcher();
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
