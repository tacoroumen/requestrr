using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Requestrr.WebApi.config;

namespace Requestrr.WebApi.RequestrrBot.ChatClients
{
    /// <summary>
    /// Reads the Discord and Matrix users the admin linked to Seerr users in the web UI.
    /// </summary>
    public static class ChatUserLinks
    {
        public static ChatUserLink[] Read()
        {
            dynamic settings = SettingsFile.Read();
            var chatClients = settings.ChatClients as JObject;

            var links = chatClients?["UserLinks"]?.ToObject<ChatUserLink[]>();

            // Earlier builds kept the links in the Matrix settings, as { MatrixUserId, OverseerrUserId }.
            if (links == null && chatClients?["Matrix"]?["UserLinks"] is JArray matrixLinks)
            {
                links = matrixLinks
                    .Select(x => new ChatUserLink { ChatUserId = (string)x["MatrixUserId"], OverseerrUserId = (string)x["OverseerrUserId"] })
                    .ToArray();
            }

            return (links ?? Array.Empty<ChatUserLink>())
                .Where(x => !string.IsNullOrWhiteSpace(x?.ChatUserId) && !string.IsNullOrWhiteSpace(x.OverseerrUserId))
                .Select(x => new ChatUserLink { ChatUserId = x.ChatUserId.Trim(), OverseerrUserId = x.OverseerrUserId.Trim() })
                .ToArray();
        }
    }
}
