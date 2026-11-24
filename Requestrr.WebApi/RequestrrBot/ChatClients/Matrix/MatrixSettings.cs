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
        public string[] ApprovalRooms { get; set; } = Array.Empty<string>();

        /// <summary>Room power level that lets people who are not linked to a Seerr user approve requests (50 is moderator).</summary>
        public int ApprovalPowerLevel { get; set; } = 50;

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
                   NotificationRooms.SequenceEqual(other.NotificationRooms) &&
                   ApprovalRooms.SequenceEqual(other.ApprovalRooms);
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
                ApprovalRooms = ReadArray(matrix["ApprovalRooms"]),
                ApprovalPowerLevel = (int?)matrix["ApprovalPowerLevel"] ?? 50,
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
