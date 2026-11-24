using System.ComponentModel.DataAnnotations;

namespace Requestrr.WebApi.Controllers.ChatClients
{
    public class ChatClientsSettingsModel
    {
        [Required]
        public string Client { get; set; }

        public string ClientId { get; set; }

        public string BotToken { get; set; }

        public string StatusMessage { get; set; }

        public string[] MonitoredChannels { get; set; }

        public string[] TvShowRoles { get; set; }

        public string[] MovieRoles { get; set; }

        public string[] MusicRoles { get; set; }

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

        public MatrixChatClientSettingsModel Matrix { get; set; }

        public string Language { get; set; }

        public string[] AvailableLanguages { get; set; }
    }

    public class MatrixChatClientSettingsModel
    {
        public string HomeserverUrl { get; set; }

        public string AccessToken { get; set; }

        public string CommandPrefix { get; set; }

        public string[] MonitoredRooms { get; set; }

        public string NotificationMode { get; set; }

        public string[] NotificationRooms { get; set; }

        public bool AutomaticallyNotifyRequesters { get; set; }

        public Requestrr.WebApi.config.MatrixUserLink[] UserLinks { get; set; }

        public string[] ApprovalRooms { get; set; }
    }

    public class MatrixTestSettingsModel
    {
        [Required]
        public string HomeserverUrl { get; set; }

        [Required]
        public string AccessToken { get; set; }
    }

    public class ChatClientTestSettingsModel
    {
        [Required]
        public string BotToken { get; set; }
    }
}
