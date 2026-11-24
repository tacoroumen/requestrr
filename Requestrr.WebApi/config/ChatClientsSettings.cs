namespace Requestrr.WebApi.config
{
    public class ChatClientsSettings
    {
        public DiscordSettings Discord { get; set; }
        public MatrixSettings Matrix { get; set; }
        public string Language { get; set; }
    }

    public class MatrixSettings
    {
        public string HomeserverUrl { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string CommandPrefix { get; set; } = "!";
        public string[] MonitoredRooms { get; set; } = System.Array.Empty<string>();
        public bool AutomaticallyNotifyRequesters { get; set; } = true;
        public string NotificationMode { get; set; } = "PrivateMessages";
        public string[] NotificationRooms { get; set; } = System.Array.Empty<string>();
        public MatrixUserLink[] UserLinks { get; set; } = System.Array.Empty<MatrixUserLink>();
        public string[] ApprovalRooms { get; set; } = System.Array.Empty<string>();
    }

    /// <summary>
    /// Links a Matrix user to an Overseerr/Seerr user, so their requests are made as that user.
    /// Seerr stores Discord IDs per user but has no Matrix field, so the admin keeps this list in Requestrr.
    /// </summary>
    public class MatrixUserLink
    {
        public string MatrixUserId { get; set; }
        public string OverseerrUserId { get; set; }
    }

    public class DiscordSettings
    {
        public string BotToken { get; set; }
        public string ClientId { get; set; }
        public string StatusMessage { get; set; }
        public string[] TvShowRoles { get; set; }
        public string[] MovieRoles { get; set; }
        public string[] MusicRoles { get; set; }
        public string[] MonitoredChannels { get; set; }
        public string[] RequestApprovalAdminRoleIds { get; set; }
        public string[] RequestApprovalAdminChannelIds { get; set; }
        public bool RequestApprovalAdminChannelAllRequests { get; set; }
        public string RequestApprovalEmojiApprove { get; set; }
        public string RequestApprovalEmojiDeny { get; set; }
        public bool RequestApprovalEnabled { get; set; }
        public bool EnableRequestsThroughDirectMessages { get; set; }
        public bool AutomaticallyNotifyRequesters { get; set; }
        public string NotificationMode { get; set; }
        public string[] NotificationChannels { get; set; }
        public bool AutomaticallyPurgeCommandMessages { get; set; }
    }
}
