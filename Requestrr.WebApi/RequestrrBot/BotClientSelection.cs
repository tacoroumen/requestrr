using System;
using System.Collections.Generic;
using System.Linq;

namespace Requestrr.WebApi.RequestrrBot
{
    /// <summary>
    /// Reads which chat clients the admin turned on in the web UI. BotClient.Client holds "Discord", "Matrix" or "Discord,Matrix";
    /// an empty value means Discord, as in settings files from before Matrix support.
    /// </summary>
    public static class BotClientSelection
    {
        public const string Discord = "Discord";
        public const string Matrix = "Matrix";

        public static bool IsKnownClient(string client)
        {
            return string.Equals(client, Discord, StringComparison.OrdinalIgnoreCase) || string.Equals(client, Matrix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDiscordEnabled() => Enabled().Contains(Discord);

        public static bool IsMatrixEnabled() => Enabled().Contains(Matrix);

        public static string Format(bool discord, bool matrix)
        {
            var clients = new List<string>();
            if (discord) clients.Add(Discord);
            if (matrix) clients.Add(Matrix);
            return string.Join(",", clients);
        }

        private static HashSet<string> Enabled()
        {
            string value;

            try
            {
                dynamic settings = SettingsFile.Read();
                value = (string)settings.BotClient?.Client;
            }
            catch
            {
                value = null;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return new HashSet<string> { Discord };
            }

            return new HashSet<string>(value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => string.Equals(x, Matrix, StringComparison.OrdinalIgnoreCase) ? Matrix : string.Equals(x, Discord, StringComparison.OrdinalIgnoreCase) ? Discord : null)
                .Where(x => x != null));
        }
    }
}
