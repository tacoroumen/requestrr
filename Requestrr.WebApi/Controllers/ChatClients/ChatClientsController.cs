using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Requestrr.WebApi.config;
using Requestrr.WebApi.RequestrrBot;

namespace Requestrr.WebApi.Controllers.ChatClients
{
    [ApiController]
    [Authorize]
    [Route("/api/chatclients")]
    public class ChatClientsController : ControllerBase
    {
        private readonly ChatClientsSettings _chatClientsSettings;
        private readonly BotClientSettings _botClientsSettings;
        private readonly ILogger<ChatClientsController> _logger;

        public ChatClientsController(
            ChatClientsSettingsProvider chatClientsSettingsProvider,
            BotClientSettingsProvider botClientSettingsProvider,
            ILogger<ChatClientsController> logger)
        {
            _logger = logger;
            _chatClientsSettings = chatClientsSettingsProvider.Provide();
            _botClientsSettings = botClientSettingsProvider.Provide();
        }

        [HttpGet()]
        public async Task<IActionResult> GetAsync()
        {
            return Ok(new ChatClientsSettingsModel
            {
                Client = BotClientSelection.Format(BotClientSelection.IsDiscordEnabled(), BotClientSelection.IsMatrixEnabled()),
                StatusMessage = _chatClientsSettings.Discord.StatusMessage,
                BotToken = _chatClientsSettings.Discord.BotToken,
                ClientId = _chatClientsSettings.Discord.ClientId,
                EnableRequestsThroughDirectMessages = _chatClientsSettings.Discord.EnableRequestsThroughDirectMessages,
                TvShowRoles = _chatClientsSettings.Discord.TvShowRoles ?? Array.Empty<string>(),
                MovieRoles = _chatClientsSettings.Discord.MovieRoles ?? Array.Empty<string>(),
                MusicRoles = _chatClientsSettings.Discord.MusicRoles ?? Array.Empty<string>(),
                RequestApprovalAdminRoleIds = _chatClientsSettings.Discord.RequestApprovalAdminRoleIds ?? Array.Empty<string>(),
                RequestApprovalAdminChannelIds = _chatClientsSettings.Discord.RequestApprovalAdminChannelIds ?? Array.Empty<string>(),
                RequestApprovalAdminChannelAllRequests = _chatClientsSettings.Discord.RequestApprovalAdminChannelAllRequests,
                RequestApprovalEmojiApprove = _chatClientsSettings.Discord.RequestApprovalEmojiApprove,
                RequestApprovalEmojiDeny = _chatClientsSettings.Discord.RequestApprovalEmojiDeny,
                RequestApprovalEnabled = _chatClientsSettings.Discord.RequestApprovalEnabled,
                MonitoredChannels = _chatClientsSettings.Discord.MonitoredChannels ?? Array.Empty<string>(),
                AutomaticallyNotifyRequesters = _chatClientsSettings.Discord.AutomaticallyNotifyRequesters,
                NotificationMode = _chatClientsSettings.Discord.NotificationMode,
                NotificationChannels = _chatClientsSettings.Discord.NotificationChannels ?? Array.Empty<string>(),
                AutomaticallyPurgeCommandMessages = _chatClientsSettings.Discord.AutomaticallyPurgeCommandMessages,
                Matrix = new MatrixChatClientSettingsModel
                {
                    HomeserverUrl = _chatClientsSettings.Matrix.HomeserverUrl,
                    AccessToken = _chatClientsSettings.Matrix.AccessToken,
                    CommandPrefix = _chatClientsSettings.Matrix.CommandPrefix,
                    MonitoredRooms = _chatClientsSettings.Matrix.MonitoredRooms,
                    NotificationMode = _chatClientsSettings.Matrix.NotificationMode,
                    NotificationRooms = _chatClientsSettings.Matrix.NotificationRooms,
                    AutomaticallyNotifyRequesters = _chatClientsSettings.Matrix.AutomaticallyNotifyRequesters,
                    ApprovalRooms = _chatClientsSettings.Matrix.ApprovalRooms ?? Array.Empty<string>(),
                },
                UserLinks = _chatClientsSettings.UserLinks ?? Array.Empty<ChatUserLink>(),
                Language = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_chatClientsSettings.Language.ToLower()),
                AvailableLanguages = GetLanguages()
            });
        }


        [HttpPost("discord/test")]
        public async Task<IActionResult> TestDiscordSettings([FromBody] ChatClientTestSettingsModel model)
        {
            try
            {
                var discord = new DiscordClient(new DiscordConfiguration
                {
                    Token = model.BotToken,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.All,
                    AutoReconnect = false
                });

                var tcs = new TaskCompletionSource<string>();

                async Task ClientErrored(DiscordClient client, ClientErrorEventArgs args)
                {
                    tcs.TrySetResult("The bot is missing [Presence Intent] and/or [Server Members Intent] and/or [Message Content Intent], you must enable them in the bot discord settings and then restart the bot, check the wiki if you are unsure how to do this");
                }

                async Task SocketErrored(DiscordClient client, SocketErrorEventArgs args)
                {
                    tcs.TrySetResult("The bot is missing [Presence Intent] and/or [Server Members Intent] and/or [Message Content Intent], you must enable them in the bot discord settings and then restart the bot, check the wiki if you are unsure how to do this");
                }

                async Task Connected(DiscordClient client, ReadyEventArgs args)
                {
                    tcs.TrySetResult(string.Empty);
                }

                async Task Closed(DiscordClient client, SocketCloseEventArgs args)
                {
                    tcs.TrySetResult("The bot is missing [Presence Intent] and/or [Server Members Intent] and/or [Message Content Intent], you must enable them in the bot discord settings and then restart the bot, check the wiki if you are unsure how to do this");
                }

                discord.ClientErrored += ClientErrored;
                discord.Ready += Connected;
                discord.SocketErrored += SocketErrored;
                discord.SocketClosed += Closed;

                await discord.ConnectAsync();

                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(15));
                    tcs.TrySetException(new Exception());
                });

                var message = await tcs.Task;

                await discord.DisconnectAsync();
                discord.Dispose();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    return BadRequest(message);
                }

                return Ok(new { ok = true });
            }
            catch (System.Exception)
            {
                return BadRequest($"Invalid bot token");
            }
        }

        [HttpGet("overseerr-users")]
        public async Task<IActionResult> GetOverseerrUsersAsync([FromServices] RequestrrServices services)
        {
            try
            {
                var users = await services.OverseerrClient.GetUsersAsync();
                return Ok(users.Select(x => new { id = x.Id.ToString(), name = string.IsNullOrWhiteSpace(x.DisplayName) ? $"User {x.Id}" : x.DisplayName }));
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not list Overseerr users: {ex.Message}");
                return BadRequest("Could not load the Seerr users. Set up Overseerr/Seerr under Download Clients first.");
            }
        }

        [HttpPost("matrix/test")]
        public async Task<IActionResult> TestMatrixSettings([FromBody] MatrixTestSettingsModel model)
        {
            if (!Uri.TryCreate(model.HomeserverUrl?.Trim(), UriKind.Absolute, out var homeserver) || (homeserver.Scheme != Uri.UriSchemeHttps && homeserver.Scheme != Uri.UriSchemeHttp))
            {
                return BadRequest("Enter the homeserver URL, starting with https:// or http://.");
            }

            if (string.IsNullOrWhiteSpace(model.AccessToken))
            {
                return BadRequest("Enter the access token of the bot account.");
            }

            var homeserverUrl = homeserver.GetLeftPart(UriPartial.Path).TrimEnd('/');

            try
            {
                using var client = new Requestrr.WebApi.RequestrrBot.ChatClients.Matrix.MatrixApiClient(homeserverUrl, model.AccessToken.Trim());
                using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                var userId = await client.WhoAmIAsync(timeout.Token);
                return Ok(new { ok = true, userId });
            }
            catch (Requestrr.WebApi.RequestrrBot.ChatClients.Matrix.MatrixApiException ex) when (ex.ErrorCode == "M_UNKNOWN_TOKEN" || ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return BadRequest("The homeserver rejected the access token.");
            }
            catch (Requestrr.WebApi.RequestrrBot.ChatClients.Matrix.MatrixApiException ex)
            {
                _logger.LogWarning($"Matrix test: {homeserverUrl} answered HTTP {(int)ex.StatusCode} {ex.ErrorCode} {ex.Message}");
                if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden || ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    return BadRequest($"{homeserverUrl} blocked the request with HTTP {(int)ex.StatusCode}. A firewall or proxy such as Cloudflare may be blocking bots: allow the user agent {Requestrr.WebApi.RequestrrBot.ChatClients.Matrix.MatrixApiClient.UserAgent}.");
                }

                return BadRequest($"{homeserverUrl} answered with HTTP {(int)ex.StatusCode}, so it may not be the Matrix client API. Use the address your Matrix client (Element) connects to.");
            }
            catch (Newtonsoft.Json.JsonReaderException)
            {
                _logger.LogWarning($"Matrix test: {homeserverUrl} did not answer with JSON.");
                return BadRequest($"{homeserverUrl} answered with a web page instead of the Matrix API. Use the address your Matrix client (Element) connects to, usually https://matrix.your-domain.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"Matrix test: no answer from {homeserverUrl} within 10 seconds.");
                return BadRequest($"No answer from {homeserverUrl} within 10 seconds. Check that Requestrr can reach that address from where it runs (DNS, firewall, or NAT loopback).");
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _logger.LogWarning(ex, $"Matrix test: could not connect to {homeserverUrl}: {ex.Message}");
                return BadRequest($"Could not connect to {homeserverUrl}: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Matrix test against {homeserverUrl} failed: {ex.Message}");
                return BadRequest($"The Matrix test failed: {ex.Message}");
            }
        }

        [HttpPost()]
        public async Task<IActionResult> SaveAsync([FromBody] ChatClientsSettingsModel model)
        {
            var requestedClients = (model.Client ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!requestedClients.Any() || requestedClients.Any(x => !BotClientSelection.IsKnownClient(x)))
            {
                return BadRequest("Invalid client was selected");
            }

            var discordEnabled = requestedClients.Contains(BotClientSelection.Discord, StringComparer.OrdinalIgnoreCase);
            var matrixEnabled = requestedClients.Contains(BotClientSelection.Matrix, StringComparer.OrdinalIgnoreCase);

            if (model.Matrix != null)
            {
                var matrixError = ValidateAndApplyMatrix(model.Matrix, matrixEnabled);

                if (matrixError != null)
                {
                    return BadRequest(matrixError);
                }
            }
            else if (matrixEnabled)
            {
                return BadRequest("Matrix settings are missing.");
            }

            var userLinksError = ValidateAndApplyUserLinks(model.UserLinks);

            if (userLinksError != null)
            {
                return BadRequest(userLinksError);
            }

            _botClientsSettings.Client = BotClientSelection.Format(discordEnabled, matrixEnabled);

            if (!discordEnabled)
            {
                // Keep the stored Discord settings as they are, so turning Discord back on restores them.
                _chatClientsSettings.Language = model.Language;
                ChatClientsSettingsRepository.Update(_botClientsSettings, _chatClientsSettings);
                return Ok(new { ok = true });
            }

            if (string.IsNullOrWhiteSpace(model.ClientId) || string.IsNullOrWhiteSpace(model.BotToken))
            {
                return BadRequest("The Discord application id and bot token are required.");
            }

            if (model.TvShowRoles.Any(x => !ulong.TryParse(x, System.Globalization.NumberStyles.Integer, null, out _)))
            {
                return BadRequest("Invalid tv show roles, please make sure to enter the discord role ids.");
            }

            if (model.MovieRoles.Any(x => !ulong.TryParse(x, System.Globalization.NumberStyles.Integer, null, out _)))
            {
                return BadRequest("Invalid movie roles, please make sure to enter the discord role ids.");
            }

            if (model.MusicRoles.Any(x => !ulong.TryParse(x, System.Globalization.NumberStyles.Integer, null, out _)))
            {
                return BadRequest("Invalid music roles, please make sure to enter the discord role ids.");
            }

            if (model.NotificationChannels.Any(x => !ulong.TryParse(x, System.Globalization.NumberStyles.Integer, null, out _)))
            {
                return BadRequest("Invalid notification channels, please make sure to enter the discord channel ids.");
            }

            if ((model.RequestApprovalAdminRoleIds ?? Array.Empty<string>()).Any(x => !ulong.TryParse(x, System.Globalization.NumberStyles.Integer, null, out _)))
            {
                return BadRequest("Invalid admin role ids, please make sure to enter the discord role ids.");
            }

            if ((model.RequestApprovalAdminChannelIds ?? Array.Empty<string>()).Any(x => !ulong.TryParse(x, System.Globalization.NumberStyles.Integer, null, out _)))
            {
                return BadRequest("Invalid admin channel ids, please make sure to enter the discord channel ids.");
            }

            if (model.MonitoredChannels.Any(x => !ulong.TryParse(x, System.Globalization.NumberStyles.Integer, null, out _)))
            {
                return BadRequest("Invalid monitored channels channels, please make sure to enter the monitored channel ids.");
            }

            _chatClientsSettings.Discord.BotToken = model.BotToken.Trim();
            _chatClientsSettings.Discord.ClientId = model.ClientId;
            _chatClientsSettings.Discord.StatusMessage = model.StatusMessage.Trim();
            _chatClientsSettings.Discord.TvShowRoles = (model.TvShowRoles ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
            _chatClientsSettings.Discord.MovieRoles = (model.MovieRoles ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
            _chatClientsSettings.Discord.MusicRoles = (model.MusicRoles ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
            _chatClientsSettings.Discord.RequestApprovalAdminRoleIds = (model.RequestApprovalAdminRoleIds ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
            _chatClientsSettings.Discord.RequestApprovalAdminChannelIds = (model.RequestApprovalAdminChannelIds ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
            _chatClientsSettings.Discord.RequestApprovalAdminChannelAllRequests = model.RequestApprovalAdminChannelAllRequests;
            _chatClientsSettings.Discord.RequestApprovalEmojiApprove = model.RequestApprovalEmojiApprove?.Trim();
            _chatClientsSettings.Discord.RequestApprovalEmojiDeny = model.RequestApprovalEmojiDeny?.Trim();
            _chatClientsSettings.Discord.RequestApprovalEnabled = model.RequestApprovalEnabled;
            _chatClientsSettings.Discord.EnableRequestsThroughDirectMessages = model.EnableRequestsThroughDirectMessages;
            _chatClientsSettings.Discord.MonitoredChannels = (model.MonitoredChannels ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

            _chatClientsSettings.Discord.AutomaticallyNotifyRequesters = model.AutomaticallyNotifyRequesters;
            _chatClientsSettings.Discord.NotificationMode = model.NotificationMode;
            _chatClientsSettings.Discord.NotificationChannels = (model.NotificationChannels ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray(); ;
            _chatClientsSettings.Discord.AutomaticallyPurgeCommandMessages = model.AutomaticallyPurgeCommandMessages;
            _chatClientsSettings.Language = model.Language;


            ChatClientsSettingsRepository.Update(_botClientsSettings, _chatClientsSettings);

            return Ok(new { ok = true });
        }

        private string ValidateAndApplyMatrix(MatrixChatClientSettingsModel matrix, bool required)
        {
            var homeserverUrl = (matrix.HomeserverUrl ?? string.Empty).Trim().TrimEnd('/');
            var accessToken = (matrix.AccessToken ?? string.Empty).Trim();
            var monitoredRooms = CleanIds(matrix.MonitoredRooms);
            var notificationRooms = CleanIds(matrix.NotificationRooms);
            var approvalRooms = CleanIds(matrix.ApprovalRooms);
            var notificationMode = matrix.NotificationMode ?? "PrivateMessages";

            if (required || !string.IsNullOrEmpty(homeserverUrl))
            {
                if (!Uri.TryCreate(homeserverUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                {
                    return "Enter the Matrix homeserver URL, starting with https:// or http://.";
                }
            }

            if (required && string.IsNullOrEmpty(accessToken))
            {
                return "The Matrix access token is required.";
            }

            if (monitoredRooms.Concat(notificationRooms).Concat(approvalRooms).Any(x => !Requestrr.WebApi.RequestrrBot.ChatClients.Matrix.MatrixIds.IsRoomId(x) && !Requestrr.WebApi.RequestrrBot.ChatClients.Matrix.MatrixIds.IsRoomAlias(x)))
            {
                return "Enter Matrix rooms as room ids (!abc:example.org) or aliases (#room:example.org).";
            }

            if (notificationMode != "Disabled" && notificationMode != "PrivateMessages" && notificationMode != "Channels")
            {
                return "Invalid Matrix notification mode.";
            }

            if (required && notificationMode == "Channels" && !notificationRooms.Any())
            {
                return "Add at least one Matrix room to send notifications to.";
            }

            var prefix = string.IsNullOrWhiteSpace(matrix.CommandPrefix) ? "!" : matrix.CommandPrefix.Trim();

            if (prefix.Length > 5 || prefix.Any(char.IsWhiteSpace))
            {
                return "The Matrix command prefix must be 1 to 5 characters without spaces.";
            }

            _chatClientsSettings.Matrix = new MatrixSettings
            {
                HomeserverUrl = homeserverUrl,
                AccessToken = accessToken,
                CommandPrefix = prefix,
                MonitoredRooms = monitoredRooms,
                NotificationMode = notificationMode,
                NotificationRooms = notificationRooms,
                AutomaticallyNotifyRequesters = matrix.AutomaticallyNotifyRequesters,
                ApprovalRooms = approvalRooms,
            };

            return null;
        }

        private string ValidateAndApplyUserLinks(ChatUserLink[] links)
        {
            var userLinks = (links ?? Array.Empty<ChatUserLink>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.ChatUserId))
                .Select(x => new ChatUserLink { ChatUserId = x.ChatUserId.Trim(), OverseerrUserId = (x.OverseerrUserId ?? string.Empty).Trim() })
                .ToArray();

            if (userLinks.Any(x => !Requestrr.WebApi.RequestrrBot.ChatClients.Matrix.MatrixIds.IsUserId(x.ChatUserId) && !ulong.TryParse(x.ChatUserId, System.Globalization.NumberStyles.Integer, null, out _)))
            {
                return "Enter linked users as a Matrix user ID (@name:example.org) or a Discord user ID (a number).";
            }

            if (userLinks.Any(x => !int.TryParse(x.OverseerrUserId, out _)))
            {
                return "Pick a Seerr user for every linked chat user.";
            }

            if (userLinks.GroupBy(x => x.ChatUserId, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            {
                return "Each chat user can be linked to one Seerr user only.";
            }

            _chatClientsSettings.UserLinks = userLinks;
            return null;
        }

        private static string[] CleanIds(string[] ids)
        {
            return (ids ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToArray();
        }

        private string[] GetLanguages()
        {
            return Directory.GetFiles(Program.CombindPath("locales"), "*.json", SearchOption.TopDirectoryOnly)
                 .Select(x => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Path.GetFileNameWithoutExtension(x).ToLower()))
                 .ToArray();
        }
    }
}
