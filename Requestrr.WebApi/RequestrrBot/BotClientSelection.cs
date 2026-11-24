using System;

namespace Requestrr.WebApi.RequestrrBot
{
    /// <summary>
    /// Reads which chat client (Discord or Matrix) the admin picked in the web UI.
    /// Only one chat client runs at a time; an empty value means Discord for older settings files.
    /// </summary>
    public static class BotClientSelection
    {
        public const string Discord = "Discord";
        public const string Matrix = "Matrix";

        public static string Current()
        {
            try
            {
                dynamic settings = SettingsFile.Read();
                string client = (string)settings.BotClient?.Client;
                return string.Equals(client, Matrix, StringComparison.OrdinalIgnoreCase) ? Matrix : Discord;
            }
            catch
            {
                return Discord;
            }
        }

        public static bool IsMatrix() => Current() == Matrix;
    }
}
