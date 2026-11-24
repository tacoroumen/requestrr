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
        public string[] ApprovalRooms { get; set; } = System.Array.Empty<string>();
        public int ApprovalPowerLevel { get; set; } = 50;
        public bool ApprovalRoomsAllRequests { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public int[] MovieRequestLevels { get; set; } = System.Array.Empty<int>();
        public int[] TvShowRequestLevels { get; set; } = System.Array.Empty<int>();
        public int[] MusicRequestLevels { get; set; } = System.Array.Empty<int>();
    }

    /// <summary>
    /// Links a Discord or Matrix user to an Overseerr/Seerr user, so their requests are made as that user.
    /// Seerr has no Matrix field, and its Discord field is per user, so the admin can keep one list for both here.
    /// </summary>
    public class ChatUserLink
    {
        public string ChatUserId { get; set; }
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
