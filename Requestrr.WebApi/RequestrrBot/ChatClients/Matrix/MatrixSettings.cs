using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Requestrr.WebApi.RequestrrBot.Notifications;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    public class MatrixSettings
    {
        public string HomeserverUrl { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string CommandPrefix { get; set; } = "!";
        public string[] MonitoredRooms { get; set; } = Array.Empty<string>();
        public bool AutomaticallyNotifyRequesters { get; set; } = true;
        public string NotificationMode { get; set; } = Notifications.NotificationMode.PrivateMessage;
        public string[] NotificationRooms { get; set; } = Array.Empty<string>();
        public config.MatrixUserLink[] UserLinks { get; set; } = Array.Empty<config.MatrixUserLink>();

        public bool IsConfigured => !string.IsNullOrWhiteSpace(HomeserverUrl) && !string.IsNullOrWhiteSpace(AccessToken);

        public override bool Equals(object obj)
        {
            return obj is MatrixSettings other &&
                   HomeserverUrl == other.HomeserverUrl &&
                   AccessToken == other.AccessToken &&
                   CommandPrefix == other.CommandPrefix &&
                   MonitoredRooms.SequenceEqual(other.MonitoredRooms) &&
                   AutomaticallyNotifyRequesters == other.AutomaticallyNotifyRequesters &&
                   NotificationMode == other.NotificationMode &&
                   NotificationRooms.SequenceEqual(other.NotificationRooms);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(HomeserverUrl, AccessToken, CommandPrefix, NotificationMode, AutomaticallyNotifyRequesters);
        }
    }

    public class MatrixSettingsProvider
    {
        public MatrixSettings Provide()
        {
            dynamic settings = SettingsFile.Read();
            var matrix = settings.ChatClients?.Matrix as JObject;

            if (matrix == null)
            {
                return new MatrixSettings();
            }

            return new MatrixSettings
            {
                HomeserverUrl = ((string)matrix["HomeserverUrl"] ?? string.Empty).Trim().TrimEnd('/'),
                AccessToken = ((string)matrix["AccessToken"] ?? string.Empty).Trim(),
                CommandPrefix = string.IsNullOrWhiteSpace((string)matrix["CommandPrefix"]) ? "!" : ((string)matrix["CommandPrefix"]).Trim(),
                MonitoredRooms = ReadArray(matrix["MonitoredRooms"]),
                AutomaticallyNotifyRequesters = (bool?)matrix["AutomaticallyNotifyRequesters"] ?? true,
                NotificationMode = (string)matrix["NotificationMode"] ?? Notifications.NotificationMode.PrivateMessage,
                NotificationRooms = ReadArray(matrix["NotificationRooms"]),
                UserLinks = (matrix["UserLinks"]?.ToObject<config.MatrixUserLink[]>() ?? Array.Empty<config.MatrixUserLink>())
                    .Where(x => !string.IsNullOrWhiteSpace(x?.MatrixUserId) && !string.IsNullOrWhiteSpace(x.OverseerrUserId))
                    .ToArray(),
            };
        }

        private static string[] ReadArray(JToken token)
        {
            return (token?.ToObject<string[]>() ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToArray();
        }
    }
}
