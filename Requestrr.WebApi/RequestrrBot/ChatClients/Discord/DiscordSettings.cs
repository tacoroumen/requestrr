using System;
using System.Linq;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Discord
{
    public class DiscordSettings
    {
        public string BotToken { get; set; }
        public string ClientID { get; set; }
        public string StatusMessage { get; set; }
        public string[] MonitoredChannels { get; set; }
        public string[] TvShowRoles { get; set; }
        public string[] MovieRoles { get; set; }
        public string[] MusicRoles { get; set; }
        public string MovieDownloadClient { get; set; }
        public int MovieDownloadClientConfigurationHash { get; set; }
        public string TvShowDownloadClient { get; set; }
        public int TvShowDownloadClientConfigurationHash { get; set; }
        public string MusicDownloadClient { get; set; }
        public int MusicDownloadClientConfigurationHash { get; set; }
        public string[] AdminUserIds { get; set; }
        public string[] AdminChannelIds { get; set; }
        public bool AdminChannelAllRequests { get; set; }
        public bool EnableRequestsThroughDirectMessages { get; set; }
        public bool AutomaticallyNotifyRequesters { get; set; }
        public string NotificationMode { get; set; }
        public string[] NotificationChannels { get; set; }
        public bool AutomaticallyPurgeCommandMessages { get; set; }

        public override bool Equals(object obj)
        {
            return obj is DiscordSettings settings &&
                   BotToken == settings.BotToken &&
                   ClientID == settings.ClientID &&
                   StatusMessage == settings.StatusMessage &&
                   (MonitoredChannels ?? Array.Empty<string>()).SequenceEqual(settings.MonitoredChannels ?? Array.Empty<string>()) &&
                   (TvShowRoles ?? Array.Empty<string>()).SequenceEqual(settings.TvShowRoles ?? Array.Empty<string>()) &&
                   (MovieRoles ?? Array.Empty<string>()).SequenceEqual(settings.MovieRoles ?? Array.Empty<string>()) &&
                   (MusicRoles ?? Array.Empty<string>()).SequenceEqual(settings.MusicRoles ?? Array.Empty<string>()) &&
                   MovieDownloadClient == settings.MovieDownloadClient &&
                   MovieDownloadClientConfigurationHash == settings.MovieDownloadClientConfigurationHash &&
                   TvShowDownloadClient == settings.TvShowDownloadClient &&
                   TvShowDownloadClientConfigurationHash == settings.TvShowDownloadClientConfigurationHash &&
                   MusicDownloadClient == settings.MusicDownloadClient &&
                   MusicDownloadClientConfigurationHash == settings.MusicDownloadClientConfigurationHash &&
                   (AdminUserIds ?? Array.Empty<string>()).SequenceEqual(settings.AdminUserIds ?? Array.Empty<string>()) &&
                   (AdminChannelIds ?? Array.Empty<string>()).SequenceEqual(settings.AdminChannelIds ?? Array.Empty<string>()) &&
                   AdminChannelAllRequests == settings.AdminChannelAllRequests &&
                   EnableRequestsThroughDirectMessages == settings.EnableRequestsThroughDirectMessages &&
                   AutomaticallyNotifyRequesters == settings.AutomaticallyNotifyRequesters &&
                   NotificationMode == settings.NotificationMode &&
                   (NotificationChannels ?? Array.Empty<string>()).SequenceEqual(settings.NotificationChannels ?? Array.Empty<string>()) &&
                   AutomaticallyPurgeCommandMessages == settings.AutomaticallyPurgeCommandMessages;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(BotToken);
            hash.Add(ClientID);
            hash.Add(StatusMessage);
            hash.Add(MonitoredChannels);
            hash.Add(MovieRoles);
            hash.Add(TvShowRoles);
            hash.Add(MusicRoles);
            hash.Add(MovieDownloadClient);
            hash.Add(MovieDownloadClientConfigurationHash);
            hash.Add(TvShowDownloadClient);
            hash.Add(TvShowDownloadClientConfigurationHash);
            hash.Add(MusicDownloadClient);
            hash.Add(MusicDownloadClientConfigurationHash);
            hash.Add(AdminUserIds);
            hash.Add(AdminChannelIds);
            hash.Add(AdminChannelAllRequests);
            hash.Add(EnableRequestsThroughDirectMessages);
            hash.Add(AutomaticallyNotifyRequesters);
            hash.Add(NotificationMode);
            hash.Add(NotificationChannels);
            hash.Add(AutomaticallyPurgeCommandMessages);
            return hash.ToHashCode();
        }
    }
}
