using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    public class MatrixApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ErrorCode { get; }

        public MatrixApiException(HttpStatusCode statusCode, string errorCode, string message)
            : base($"Matrix API error {(int)statusCode} {errorCode}: {message}")
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// A small client for the parts of the Matrix client-server API (v3) that the bot needs.
    /// It authenticates with an access token and supports unencrypted rooms only.
    /// </summary>
    public class MatrixApiClient : IDisposable
    {
        private const int MaxRateLimitRetries = 3;
        private readonly HttpClient _httpClient;
        /// <summary>
        /// Sent on every homeserver request, so admins can allow the bot through a firewall or proxy such as Cloudflare.
        /// </summary>
        public const string UserAgent = "Requestrr-Matrix/1.0";

        private readonly string _baseUrl;
        private long _transactionCounter = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public MatrixApiClient(string homeserverUrl, string accessToken)
        {
            _baseUrl = homeserverUrl.TrimEnd('/');
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }

        public async Task<string> WhoAmIAsync(CancellationToken token = default)
        {
            var response = await SendAsync(HttpMethod.Get, "/_matrix/client/v3/account/whoami", null, token);
            return (string)response["user_id"];
        }

        public async Task<JObject> SyncAsync(string since, int timeoutMs, string filterJson, CancellationToken token)
        {
            var query = new List<string> { $"timeout={timeoutMs}", $"filter={Uri.EscapeDataString(filterJson)}" };

            if (!string.IsNullOrEmpty(since))
            {
                query.Add($"since={Uri.EscapeDataString(since)}");
            }

            return await SendAsync(HttpMethod.Get, $"/_matrix/client/v3/sync?{string.Join("&", query)}", null, token);
        }

        public async Task<string> JoinRoomAsync(string roomIdOrAlias, CancellationToken token = default)
        {
            var response = await SendAsync(HttpMethod.Post, $"/_matrix/client/v3/join/{Uri.EscapeDataString(roomIdOrAlias)}", new JObject(), token);
            return (string)response["room_id"];
        }

        public async Task LeaveRoomAsync(string roomId, CancellationToken token = default)
        {
            await SendAsync(HttpMethod.Post, $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomId)}/leave", new JObject(), token);
        }

        public async Task<string> ResolveRoomAliasAsync(string alias, CancellationToken token = default)
        {
            var response = await SendAsync(HttpMethod.Get, $"/_matrix/client/v3/directory/room/{Uri.EscapeDataString(alias)}", null, token);
            return (string)response["room_id"];
        }

        public async Task<HashSet<string>> GetJoinedRoomsAsync(CancellationToken token = default)
        {
            var response = await SendAsync(HttpMethod.Get, "/_matrix/client/v3/joined_rooms", null, token);
            return new HashSet<string>(response["joined_rooms"]?.ToObject<string[]>() ?? Array.Empty<string>());
        }

        public async Task<Dictionary<string, string>> GetJoinedMembersAsync(string roomId, CancellationToken token = default)
        {
            var response = await SendAsync(HttpMethod.Get, $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomId)}/joined_members", null, token);
            var members = new Dictionary<string, string>();

            if (response["joined"] is JObject joined)
            {
                foreach (var member in joined.Properties())
                {
                    members[member.Name] = (string)member.Value["display_name"] ?? member.Name;
                }
            }

            return members;
        }

        public async Task<string> GetDisplayNameAsync(string userId, CancellationToken token = default)
        {
            try
            {
                var response = await SendAsync(HttpMethod.Get, $"/_matrix/client/v3/profile/{Uri.EscapeDataString(userId)}/displayname", null, token);
                var displayName = (string)response["displayname"];
                return string.IsNullOrWhiteSpace(displayName) ? userId : displayName;
            }
            catch (MatrixApiException)
            {
                return userId;
            }
        }

        /// <summary>
        /// Creates an unencrypted direct-message room with the user and invites them.
        /// </summary>
        public async Task<string> CreateDirectRoomAsync(string userId, CancellationToken token = default)
        {
            var body = new JObject
            {
                ["is_direct"] = true,
                ["preset"] = "trusted_private_chat",
                ["invite"] = new JArray(userId),
            };

            var response = await SendAsync(HttpMethod.Post, "/_matrix/client/v3/createRoom", body, token);
            return (string)response["room_id"];
        }

        /// <summary>
        /// Replaces the text of a message the bot sent earlier (an m.replace edit); clients show it as edited.
        /// </summary>
        public async Task<string> EditMessageAsync(string roomId, string eventId, MatrixMessage message, CancellationToken token = default)
        {
            var newContent = new JObject
            {
                ["msgtype"] = message.IsNotice ? "m.notice" : "m.text",
                ["body"] = message.PlainText,
                ["format"] = "org.matrix.custom.html",
                ["formatted_body"] = message.Html,
            };

            var content = new JObject
            {
                ["msgtype"] = newContent["msgtype"],
                ["body"] = "* " + message.PlainText,
                ["format"] = "org.matrix.custom.html",
                ["formatted_body"] = "* " + message.Html,
                ["m.new_content"] = newContent,
                ["m.relates_to"] = new JObject { ["rel_type"] = "m.replace", ["event_id"] = eventId },
            };

            return await SendEventAsync(roomId, "m.room.message", content, token);
        }

        public async Task<string> SendMessageAsync(string roomId, MatrixMessage message, CancellationToken token = default)
        {
            var content = new JObject
            {
                ["msgtype"] = message.IsNotice ? "m.notice" : "m.text",
                ["body"] = message.PlainText,
                ["format"] = "org.matrix.custom.html",
                ["formatted_body"] = message.Html,
                ["m.mentions"] = new JObject { ["user_ids"] = new JArray(message.MentionedUserIds.Distinct().ToArray()) },
            };

            if (!string.IsNullOrEmpty(message.ReplyToEventId))
            {
                content["m.relates_to"] = new JObject
                {
                    ["m.in_reply_to"] = new JObject { ["event_id"] = message.ReplyToEventId }
                };
            }

            return await SendEventAsync(roomId, "m.room.message", content, token);
        }

        public async Task<string> SendReactionAsync(string roomId, string eventId, string key, CancellationToken token = default)
        {
            var content = new JObject
            {
                ["m.relates_to"] = new JObject
                {
                    ["rel_type"] = "m.annotation",
                    ["event_id"] = eventId,
                    ["key"] = key,
                }
            };

            return await SendEventAsync(roomId, "m.reaction", content, token);
        }

        public async Task SendTypingAsync(string roomId, string userId, bool typing, CancellationToken token = default)
        {
            var body = new JObject { ["typing"] = typing };
            if (typing) body["timeout"] = 30000;
            await SendAsync(HttpMethod.Put, $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomId)}/typing/{Uri.EscapeDataString(userId)}", body, token);
        }

        private async Task<string> SendEventAsync(string roomId, string eventType, JObject content, CancellationToken token)
        {
            var transactionId = $"requestrr.{Interlocked.Increment(ref _transactionCounter)}";
            var response = await SendAsync(HttpMethod.Put, $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomId)}/send/{Uri.EscapeDataString(eventType)}/{transactionId}", content, token);
            return (string)response["event_id"];
        }

        private async Task<JObject> SendAsync(HttpMethod method, string path, JObject body, CancellationToken token)
        {
            for (var attempt = 0; ; attempt++)
            {
                using var request = new HttpRequestMessage(method, _baseUrl + path);

                if (body != null)
                {
                    request.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                }

                using var response = await _httpClient.SendAsync(request, token);
                var json = await response.Content.ReadAsStringAsync(token);

                if (response.IsSuccessStatusCode)
                {
                    return string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
                }

                JObject error = null;
                try { error = JObject.Parse(json); } catch { }

                var errorCode = (string)error?["errcode"] ?? string.Empty;
                var errorMessage = (string)error?["error"] ?? response.ReasonPhrase;

                if ((int)response.StatusCode == 429 && attempt < MaxRateLimitRetries)
                {
                    var retryAfterMs = (int?)error?["retry_after_ms"] ?? 1000;
                    await Task.Delay(Math.Clamp(retryAfterMs, 100, 30000), token);
                    continue;
                }

                throw new MatrixApiException(response.StatusCode, errorCode, errorMessage);
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
