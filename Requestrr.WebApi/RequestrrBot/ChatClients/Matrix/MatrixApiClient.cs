using System;
using System.Collections.Concurrent;
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

        /// <summary>
        /// The user's power level in a room (0 for normal users, 50 for moderators, 100 for admins by default).
        /// </summary>
        /// <summary>
        /// Tells whether end-to-end encryption is turned on in the room (it has an m.room.encryption state event).
        /// </summary>
        public async Task<bool> IsRoomEncryptedAsync(string roomId, CancellationToken token = default)
        {
            try
            {
                var encryption = await SendAsync(HttpMethod.Get, $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomId)}/state/m.room.encryption", null, token);
                return !string.IsNullOrEmpty((string)encryption["algorithm"]);
            }
            catch (MatrixApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<int> GetPowerLevelAsync(string roomId, string userId, CancellationToken token = default)
        {
            var levels = await SendAsync(HttpMethod.Get, $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomId)}/state/m.room.power_levels", null, token);
            var userLevel = (int?)levels["users"]?[userId];

            if (userLevel.HasValue)
            {
                return userLevel.Value;
            }

            // From room version 12, the room creators outrank everyone and are left out of the power levels list.
            var create = await SendAsync(HttpMethod.Get, $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomId)}/state/m.room.create/?format=event", null, token);
            var roomVersion = (string)(create["content"]?["room_version"]) ?? "1";
            var creators = new HashSet<string> { (string)create["sender"] };

            foreach (var additional in (create["content"]?["additional_creators"] as JArray) ?? new JArray())
            {
                creators.Add((string)additional);
            }

            if (int.TryParse(roomVersion, out var version) && version >= 12 && creators.Contains(userId))
            {
                return int.MaxValue;
            }

            return (int?)levels["users_default"] ?? 0;
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
            if (!string.IsNullOrWhiteSpace(message.ImageUrl))
            {
                await TrySendImageAsync(roomId, message.ImageUrl, token);
            }

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

        private const int MaxImageBytes = 5 * 1024 * 1024;
        private static readonly HttpClient ImageDownloader = CreateImageDownloader();
        private static readonly ConcurrentDictionary<string, (string Mxc, string ContentType, long Size)> UploadedImages = new ConcurrentDictionary<string, (string, string, long)>();

        private static HttpClient CreateImageDownloader()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            return client;
        }

        /// <summary>
        /// Downloads a poster, uploads it to the homeserver once and sends it as an m.image. Failures are ignored: the text still goes out.
        /// </summary>
        private async Task TrySendImageAsync(string roomId, string imageUrl, CancellationToken token)
        {
            try
            {
                if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    return;
                }

                var cacheKey = _baseUrl + "|" + imageUrl;

                if (!UploadedImages.TryGetValue(cacheKey, out var uploaded))
                {
                    using var response = await ImageDownloader.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
                    if (!response.IsSuccessStatusCode) return;

                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                    if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return;
                    if (response.Content.Headers.ContentLength > MaxImageBytes) return;

                    var bytes = await response.Content.ReadAsByteArrayAsync(token);
                    if (bytes.Length == 0 || bytes.Length > MaxImageBytes) return;

                    var fileName = System.IO.Path.GetFileName(uri.AbsolutePath);
                    if (string.IsNullOrWhiteSpace(fileName)) fileName = "poster.jpg";

                    using var upload = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/_matrix/media/v3/upload?filename={Uri.EscapeDataString(fileName)}");
                    upload.Content = new ByteArrayContent(bytes);
                    upload.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                    using var uploadResponse = await _httpClient.SendAsync(upload, token);
                    if (!uploadResponse.IsSuccessStatusCode) return;

                    var mxc = (string)JObject.Parse(await uploadResponse.Content.ReadAsStringAsync(token))["content_uri"];
                    if (string.IsNullOrEmpty(mxc)) return;

                    uploaded = (mxc, contentType, bytes.Length);
                    UploadedImages[cacheKey] = uploaded;
                }

                var content = new JObject
                {
                    ["msgtype"] = "m.image",
                    ["body"] = "poster",
                    ["url"] = uploaded.Mxc,
                    ["info"] = new JObject { ["mimetype"] = uploaded.ContentType, ["size"] = uploaded.Size },
                };

                await SendEventAsync(roomId, "m.room.message", content, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // A missing poster should never block the reply.
            }
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
