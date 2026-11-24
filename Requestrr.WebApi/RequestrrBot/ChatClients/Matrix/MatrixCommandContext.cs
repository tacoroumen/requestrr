using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Requestrr.WebApi.RequestrrBot.Locale;

namespace Requestrr.WebApi.RequestrrBot.ChatClients.Matrix
{
    /// <summary>
    /// The room, user and connection behind one command, plus helpers to reply and to ask follow-up questions.
    /// </summary>
    public class MatrixCommandContext
    {
        public const string ConfirmReaction = "✅";
        public const string CancelReaction = "❌";

        /// <summary>
        /// Keycap emoji for 1 to 10, in the form Element sends when a user taps them.
        /// </summary>
        public static readonly IReadOnlyList<string> NumberReactions = new[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" };

        public MatrixApiClient Client { get; }
        public MatrixConversationStore Conversations { get; }
        public IMatrixWorkflowProvider Workflows { get; }
        public string RoomId { get; }
        public string UserId { get; }
        public string DisplayName { get; }

        public MatrixCommandContext(MatrixApiClient client, MatrixConversationStore conversations, IMatrixWorkflowProvider workflows, string roomId, string userId, string displayName)
        {
            Client = client;
            Conversations = conversations;
            Workflows = workflows;
            RoomId = roomId;
            UserId = userId;
            DisplayName = displayName;
        }

        public async Task<string> SendAsync(MatrixMessage message)
        {
            return await Client.SendMessageAsync(RoomId, message);
        }

        public Task<string> SendAsync(string markdown)
        {
            return SendAsync(MatrixMessage.FromMarkdown(markdown));
        }

        /// <summary>
        /// Sends a numbered list and waits for the user to reply with a number (or react with a keycap emoji).
        /// </summary>
        public async Task AskToChooseAsync(MatrixMessage message, IReadOnlyList<string> labels, IReadOnlyList<Func<Task>> choices)
        {
            message.AppendNumberedList(labels)
                   .AppendMarkdown(Language.Current.MatrixSelectionPrompt.Replace(LanguageTokens.Count, labels.Count.ToString()));

            var eventId = await SendAsync(message);
            Conversations.Set(RoomId, UserId, new MatrixPrompt { PromptEventId = eventId, Choices = choices.ToArray() });
            AddButtons(eventId, NumberReactions.Take(labels.Count).Append(CancelReaction));
        }

        /// <summary>
        /// Sends a yes/no question and runs the action when the user replies "yes" or reacts with ✅.
        /// </summary>
        public async Task AskToConfirmAsync(MatrixMessage message, Func<Task> onConfirm)
        {
            message.AppendLine().AppendMarkdown(Language.Current.MatrixConfirmPrompt);

            var eventId = await SendAsync(message);
            Conversations.Set(RoomId, UserId, new MatrixPrompt { PromptEventId = eventId, OnConfirm = onConfirm });
            AddButtons(eventId, new[] { ConfirmReaction, CancelReaction });
        }

        /// <summary>
        /// Adds the answers as reactions under the prompt, so the user can tap one instead of typing.
        /// Runs in the background: the prompt already accepts answers, and a slow or rate-limited homeserver must not hold up the reply.
        /// </summary>
        private void AddButtons(string eventId, IEnumerable<string> keys)
        {
            if (string.IsNullOrEmpty(eventId)) return;

            var keyList = keys.ToArray();

            _ = Task.Run(async () =>
            {
                foreach (var key in keyList)
                {
                    try
                    {
                        await Client.SendReactionAsync(RoomId, eventId, key);
                    }
                    catch
                    {
                        // Typed answers still work without the buttons.
                        return;
                    }
                }
            });
        }

        /// <summary>
        /// Asks a free-text question and passes the user's next message in this room to the action.
        /// </summary>
        public async Task AskForTextAsync(MatrixMessage message, Func<string, Task> onText)
        {
            var eventId = await SendAsync(message);
            Conversations.Set(RoomId, UserId, new MatrixPrompt { PromptEventId = eventId, OnText = onText });
            AddButtons(eventId, new[] { CancelReaction });
        }

        public void EndConversation()
        {
            Conversations.Clear(RoomId, UserId);
        }
    }
}
