namespace Requestrr.WebApi.RequestrrBot.ChatClients
{
    /// <summary>
    /// A Discord or Matrix user the bot can see, offered as a suggestion when linking accounts to Seerr users.
    /// </summary>
    public class ChatUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Platform { get; set; }
        public string Detail { get; set; }
    }
}
