namespace Requestrr.WebApi.RequestrrBot.ChatClients.Discord
{
    public static class DiscordConstants
    {
        public const int MaxEmbedLength = 1000;
        public const int MaxMessageLength = 2000;
        public const string OverseerrRequestIdMarker = "Overseerr Request ID:";

        /// <summary>
        /// Discord user IDs are numeric snowflakes. Other chat clients (Matrix: "@user:server") share the notification store.
        /// </summary>
        public static bool IsDiscordUserId(string userId) => ulong.TryParse(userId, out _);
    }
}
